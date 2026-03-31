#!/usr/bin/env python3
"""
Gera INSERTs SQL para a tabela cest_codes a partir do JSON ncm_cest.json.
Uso: python generate-cest-seed.py [schema_name]
     python generate-cest-seed.py tenant_demo
"""
import sys
import os
import io
import json

# Force UTF-8 output
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8')

schema = sys.argv[1] if len(sys.argv) > 1 else 'tenant_demo'
json_path = os.path.join(os.path.dirname(__file__), '..', 'json', 'ncm_cest.json')

with open(json_path, encoding='utf-8') as f:
    data = json.load(f)

print(f"-- CEST codes seed for schema: {schema}")
print(f"-- Generated from: ncm_cest.json")
print(f"-- Records: {len(data)}\n")

for record in data:
    formatted = record.get('cest', '').strip()
    if not formatted:
        continue

    # Normalize: remove dots to get 7-digit code e.g. "01.001.00" -> "0100100"
    code = formatted.replace('.', '')
    if len(code) != 7:
        continue

    description_text = record.get('descricao', '').strip()
    desc_combined = f"{formatted} - {description_text}"
    segment = record.get('segmento', '').strip()
    ncm_codes_ref = record.get('ncm', '').strip()

    # Escape single quotes
    desc_escaped = desc_combined.replace("'", "''")
    formatted_escaped = formatted.replace("'", "''")
    segment_escaped = segment.replace("'", "''")
    ncm_escaped = ncm_codes_ref.replace("'", "''")

    print(
        f"INSERT INTO {schema}.cest_codes (code, formatted_code, description, segment, ncm_codes) "
        f"VALUES ('{code}', '{formatted_escaped}', '{desc_escaped}', '{segment_escaped}', '{ncm_escaped}') "
        f"ON CONFLICT (code) DO UPDATE SET "
        f"formatted_code = EXCLUDED.formatted_code, "
        f"description = EXCLUDED.description, "
        f"segment = EXCLUDED.segment, "
        f"ncm_codes = EXCLUDED.ncm_codes;"
    )

print(f"\n-- Total: {len(data)} CEST codes processed")
