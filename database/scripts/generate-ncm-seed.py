#!/usr/bin/env python3
"""
Gera INSERTs SQL para a tabela ncm_codes a partir do Excel de NCM vigente.
Uso: python generate-ncm-seed.py [schema_name]
     python generate-ncm-seed.py tenant_demo
"""
import sys
import os
import io
import openpyxl

# Force UTF-8 output
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8')

schema = sys.argv[1] if len(sys.argv) > 1 else 'tenant_demo'
excel_path = os.path.join(os.path.dirname(__file__), '..', 'excel', 'Tabela_NCM_Vigente_20260330.xlsx')

wb = openpyxl.load_workbook(excel_path, read_only=True)
ws = wb.active

print(f"-- NCM codes seed for schema: {schema}")
print(f"-- Generated from: Tabela_NCM_Vigente_20260330.xlsx")
print(f"-- Only full 8-digit NCMs are included (10.515 records)\n")

count = 0
for row in ws.iter_rows(min_row=6, values_only=True):
    raw_code = row[0]
    description_text = row[1]

    if not raw_code or not description_text:
        continue

    # Normalize: remove dots to get 8-digit code
    normalized = str(raw_code).replace('.', '').replace('-', '')
    if len(normalized) != 8:
        continue

    formatted = str(raw_code)  # keep original formatting e.g. "0101.21.00"
    # Strip leading dashes/spaces from hierarchical indentation in the Excel
    clean_desc = description_text.lstrip('- ').strip()
    desc_combined = f"{formatted} - {clean_desc}"

    # Escape single quotes
    desc_escaped = desc_combined.replace("'", "''")
    formatted_escaped = formatted.replace("'", "''")

    print(
        f"INSERT INTO {schema}.ncm_codes (code, formatted_code, description) "
        f"VALUES ('{normalized}', '{formatted_escaped}', '{desc_escaped}') "
        f"ON CONFLICT (code) DO UPDATE SET "
        f"formatted_code = EXCLUDED.formatted_code, "
        f"description = EXCLUDED.description;"
    )
    count += 1

print(f"\n-- Total: {count} NCM codes inserted")
wb.close()
