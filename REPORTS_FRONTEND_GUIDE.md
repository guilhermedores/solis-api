# 📊 Sistema de Relatórios Genéricos - Guia de Implementação Frontend

## Visão Geral

O backend implementou um sistema completo de relatórios genéricos baseado em metadados. O sistema permite listar, configurar filtros, executar relatórios e exportar para CSV sem necessidade de criar telas específicas para cada relatório.

---

## 🔌 API Endpoints

### 1. Listar Relatórios Disponíveis

**Endpoint:** `GET /api/reports`

**Query Params:**
- `category` (opcional): Filtra por categoria

**Headers:**
```http
Authorization: Bearer {token}
X-Tenant-Subdomain: demo
```

**Exemplo de Request:**
```bash
curl -X GET "http://localhost:5287/api/reports?category=Produtos" \
  -H "Authorization: Bearer {token}" \
  -H "X-Tenant-Subdomain: demo"
```

**Resposta:**
```json
{
  "reports": [
    {
      "name": "products_list",
      "displayName": "Relatório de Produtos",
      "description": "Lista completa de produtos cadastrados com preços e custos",
      "category": "Produtos"
    }
  ]
}
```

---

### 2. Obter Metadados do Relatório

**Endpoint:** `GET /api/reports/{reportName}/metadata`

**Path Params:**
- `reportName`: Nome identificador do relatório

**Headers:**
```http
Authorization: Bearer {token}
X-Tenant-Subdomain: demo
```

**Exemplo de Request:**
```bash
curl -X GET "http://localhost:5287/api/reports/products_list/metadata" \
  -H "Authorization: Bearer {token}" \
  -H "X-Tenant-Subdomain: demo"
```

**Resposta:**
```json
{
  "name": "products_list",
  "displayName": "Relatório de Produtos",
  "description": "Lista completa de produtos cadastrados",
  "category": "Produtos",
  "fields": [
    {
      "name": "code",
      "displayName": "Código",
      "fieldType": "string",
      "formatMask": null,
      "aggregation": null,
      "visible": true,
      "sortable": true,
      "filterable": true
    },
    {
      "name": "description",
      "displayName": "Descrição",
      "fieldType": "string",
      "formatMask": null,
      "aggregation": null,
      "visible": true,
      "sortable": true,
      "filterable": true
    },
    {
      "name": "active",
      "displayName": "Ativo",
      "fieldType": "boolean",
      "formatMask": null,
      "aggregation": null,
      "visible": true,
      "sortable": true,
      "filterable": true
    }
  ],
  "filters": [
    {
      "name": "active",
      "displayName": "Status",
      "fieldType": "select",
      "filterType": "equals",
      "defaultValue": null,
      "required": false,
      "options": [
        { "value": "true", "label": "Ativo" },
        { "value": "false", "label": "Inativo" }
      ]
    },
    {
      "name": "description",
      "displayName": "Descrição",
      "fieldType": "string",
      "filterType": "contains",
      "defaultValue": null,
      "required": false,
      "options": []
    },
    {
      "name": "code",
      "displayName": "Código",
      "fieldType": "string",
      "filterType": "contains",
      "defaultValue": null,
      "required": false,
      "options": []
    }
  ]
}
```

**Tipos de Campos (`fieldType`):**
- `string` - Texto
- `number` - Número inteiro
- `decimal` - Número decimal
- `boolean` - Verdadeiro/Falso
- `date` - Data
- `select` - Lista de opções

**Tipos de Filtros (`filterType`):**
- `equals` - Igualdade exata
- `contains` - Contém texto (case-insensitive)
- `greater_than` - Maior que
- `less_than` - Menor que
- `between` - Entre dois valores

---

### 3. Executar Relatório

**Endpoint:** `POST /api/reports/{reportName}/execute`

**Path Params:**
- `reportName`: Nome identificador do relatório

**Headers:**
```http
Authorization: Bearer {token}
X-Tenant-Subdomain: demo
Content-Type: application/json
```

**Body:**
```json
{
  "filters": {
    "active": "true",
    "description": "Produto"
  },
  "page": 1,
  "pageSize": 20,
  "sortBy": "description",
  "sortDirection": "ASC"
}
```

**Parâmetros do Body:**
- `filters` (objeto, opcional): Mapa de filtros (chave = nome do filtro, valor = valor do filtro)
- `page` (número, padrão: 1): Página atual
- `pageSize` (número, padrão: 50): Quantidade de registros por página
- `sortBy` (string, opcional): Campo para ordenação
- `sortDirection` (string, padrão: "ASC"): Direção da ordenação ("ASC" ou "DESC")

**Exemplo de Request:**
```bash
curl -X POST "http://localhost:5287/api/reports/products_list/execute" \
  -H "Authorization: Bearer {token}" \
  -H "X-Tenant-Subdomain: demo" \
  -H "Content-Type: application/json" \
  -d '{
    "filters": { "active": "true" },
    "page": 1,
    "pageSize": 20
  }'
```

**Resposta:**
```json
{
  "data": [
    {
      "code": "000001",
      "barcode": "7891234567890",
      "description": "Produto A",
      "unit_code": "UN",
      "brand_name": "SEM MARCA",
      "group_name": "GERAL",
      "active": true
    },
    {
      "code": "000002",
      "barcode": "7891234567891",
      "description": "Produto B",
      "unit_code": "UN",
      "brand_name": "SEM MARCA",
      "group_name": "GERAL",
      "active": true
    }
  ],
  "pagination": {
    "page": 1,
    "pageSize": 20,
    "totalRecords": 2,
    "totalPages": 1
  }
}
```

---

### 4. Exportar para CSV

**Endpoint:** `POST /api/reports/{reportName}/export`

**Path Params:**
- `reportName`: Nome identificador do relatório

**Headers:**
```http
Authorization: Bearer {token}
X-Tenant-Subdomain: demo
Content-Type: application/json
```

**Body:**
```json
{
  "filters": {
    "active": "true"
  }
}
```

**Parâmetros do Body:**
- `filters` (objeto, opcional): Mapa de filtros para aplicar na exportação

**Exemplo de Request:**
```bash
curl -X POST "http://localhost:5287/api/reports/products_list/export" \
  -H "Authorization: Bearer {token}" \
  -H "X-Tenant-Subdomain: demo" \
  -H "Content-Type: application/json" \
  -d '{ "filters": { "active": "true" } }' \
  --output products.csv
```

**Resposta:**
- Content-Type: `text/csv`
- Arquivo CSV com encoding UTF-8
- Nome do arquivo: `{reportName}_{timestamp}.csv`

**Exemplo de CSV:**
```csv
"Código","Código de Barras","Descrição","Unidade","Marca","Grupo","Ativo"
000001,7891234567890,Produto A,UN,SEM MARCA,GERAL,True
000002,7891234567891,Produto B,UN,SEM MARCA,GERAL,True
```

---

## 💻 Implementação Frontend

### TypeScript Interfaces

```typescript
// types/report.types.ts

export interface Report {
  name: string;
  displayName: string;
  description: string;
  category: string;
}

export interface ReportMetadata extends Report {
  fields: ReportField[];
  filters: ReportFilter[];
}

export interface ReportField {
  name: string;
  displayName: string;
  fieldType: 'string' | 'number' | 'decimal' | 'boolean' | 'date' | 'select';
  formatMask?: string;
  aggregation?: string;
  visible: boolean;
  sortable: boolean;
  filterable: boolean;
}

export interface ReportFilter {
  name: string;
  displayName: string;
  fieldType: 'string' | 'number' | 'decimal' | 'boolean' | 'date' | 'select';
  filterType: 'equals' | 'contains' | 'greater_than' | 'less_than' | 'between';
  defaultValue?: string;
  required: boolean;
  options: FilterOption[];
}

export interface FilterOption {
  value: string;
  label: string;
}

export interface ReportExecutionRequest {
  filters?: Record<string, string>;
  page?: number;
  pageSize?: number;
  sortBy?: string;
  sortDirection?: 'ASC' | 'DESC';
}

export interface ReportResponse {
  data: Record<string, any>[];
  pagination: {
    page: number;
    pageSize: number;
    totalRecords: number;
    totalPages: number;
  };
}
```

---

### Service de Relatórios

```typescript
// services/reportService.ts

import { api } from './api';
import type { 
  Report, 
  ReportMetadata, 
  ReportExecutionRequest, 
  ReportResponse 
} from '@/types/report.types';

export const reportService = {
  /**
   * Lista todos os relatórios disponíveis
   */
  async listReports(category?: string): Promise<{ reports: Report[] }> {
    const url = category 
      ? `/api/reports?category=${encodeURIComponent(category)}`
      : '/api/reports';
    
    const response = await api.get<{ reports: Report[] }>(url);
    return response.data;
  },

  /**
   * Obtém os metadados de um relatório específico
   */
  async getMetadata(reportName: string): Promise<ReportMetadata> {
    const response = await api.get<ReportMetadata>(
      `/api/reports/${reportName}/metadata`
    );
    return response.data;
  },

  /**
   * Executa um relatório com filtros e paginação
   */
  async execute(
    reportName: string, 
    request: ReportExecutionRequest
  ): Promise<ReportResponse> {
    const response = await api.post<ReportResponse>(
      `/api/reports/${reportName}/execute`,
      request
    );
    return response.data;
  },

  /**
   * Exporta um relatório para CSV
   */
  async exportCsv(
    reportName: string, 
    filters?: Record<string, string>
  ): Promise<void> {
    const response = await api.post(
      `/api/reports/${reportName}/export`,
      { filters },
      { responseType: 'blob' }
    );
    
    // Download automático do arquivo
    const url = window.URL.createObjectURL(new Blob([response.data]));
    const link = document.createElement('a');
    link.href = url;
    link.setAttribute(
      'download', 
      `${reportName}_${new Date().toISOString().slice(0, 10)}.csv`
    );
    document.body.appendChild(link);
    link.click();
    link.remove();
    window.URL.revokeObjectURL(url);
  }
};
```

---

### Componente de Filtros Dinâmicos

```tsx
// components/ReportFilters.tsx

import React from 'react';
import { ReportFilter } from '@/types/report.types';

interface ReportFiltersProps {
  filters: ReportFilter[];
  values: Record<string, string>;
  onChange: (name: string, value: string) => void;
  onClear: () => void;
  onSearch: () => void;
  loading?: boolean;
}

export function ReportFilters({
  filters,
  values,
  onChange,
  onClear,
  onSearch,
  loading = false
}: ReportFiltersProps) {
  const renderFilter = (filter: ReportFilter) => {
    const commonProps = {
      id: filter.name,
      name: filter.name,
      value: values[filter.name] || '',
      onChange: (e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement>) =>
        onChange(filter.name, e.target.value),
      required: filter.required,
      disabled: loading,
      className: 'w-full px-3 py-2 border rounded-md focus:ring-2 focus:ring-blue-500'
    };

    switch (filter.fieldType) {
      case 'select':
        return (
          <div key={filter.name}>
            <label htmlFor={filter.name} className="block text-sm font-medium mb-1">
              {filter.displayName}
              {filter.required && <span className="text-red-500 ml-1">*</span>}
            </label>
            <select {...commonProps}>
              <option value="">Todos</option>
              {filter.options.map(opt => (
                <option key={opt.value} value={opt.value}>
                  {opt.label}
                </option>
              ))}
            </select>
          </div>
        );

      case 'boolean':
        return (
          <div key={filter.name}>
            <label htmlFor={filter.name} className="block text-sm font-medium mb-1">
              {filter.displayName}
              {filter.required && <span className="text-red-500 ml-1">*</span>}
            </label>
            <select {...commonProps}>
              <option value="">Todos</option>
              <option value="true">Sim</option>
              <option value="false">Não</option>
            </select>
          </div>
        );

      case 'date':
        return (
          <div key={filter.name}>
            <label htmlFor={filter.name} className="block text-sm font-medium mb-1">
              {filter.displayName}
              {filter.required && <span className="text-red-500 ml-1">*</span>}
            </label>
            <input type="date" {...commonProps} />
          </div>
        );

      case 'number':
      case 'decimal':
        return (
          <div key={filter.name}>
            <label htmlFor={filter.name} className="block text-sm font-medium mb-1">
              {filter.displayName}
              {filter.required && <span className="text-red-500 ml-1">*</span>}
            </label>
            <input
              type="number"
              step={filter.fieldType === 'decimal' ? '0.01' : '1'}
              placeholder={getFilterPlaceholder(filter)}
              {...commonProps}
            />
          </div>
        );

      default: // string
        return (
          <div key={filter.name}>
            <label htmlFor={filter.name} className="block text-sm font-medium mb-1">
              {filter.displayName}
              {filter.required && <span className="text-red-500 ml-1">*</span>}
            </label>
            <input
              type="text"
              placeholder={getFilterPlaceholder(filter)}
              {...commonProps}
            />
          </div>
        );
    }
  };

  const getFilterPlaceholder = (filter: ReportFilter): string => {
    switch (filter.filterType) {
      case 'contains':
        return `Pesquisar ${filter.displayName.toLowerCase()}...`;
      case 'equals':
        return `${filter.displayName}`;
      case 'greater_than':
        return `Maior que...`;
      case 'less_than':
        return `Menor que...`;
      default:
        return `Filtrar por ${filter.displayName.toLowerCase()}`;
    }
  };

  return (
    <div className="bg-white p-4 rounded-lg shadow">
      <h3 className="text-lg font-semibold mb-4">Filtros</h3>
      
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4 mb-4">
        {filters.map(filter => renderFilter(filter))}
      </div>

      <div className="flex gap-2">
        <button
          onClick={onSearch}
          disabled={loading}
          className="px-4 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700 disabled:opacity-50 disabled:cursor-not-allowed"
        >
          {loading ? '🔄 Pesquisando...' : '🔍 Pesquisar'}
        </button>
        <button
          onClick={onClear}
          disabled={loading}
          className="px-4 py-2 bg-gray-200 text-gray-700 rounded-md hover:bg-gray-300 disabled:opacity-50 disabled:cursor-not-allowed"
        >
          🗑️ Limpar
        </button>
      </div>
    </div>
  );
}
```

---

### Componente de Tabela de Dados

```tsx
// components/ReportTable.tsx

import React from 'react';
import { ReportField } from '@/types/report.types';

interface ReportTableProps {
  fields: ReportField[];
  data: Record<string, any>[];
  loading?: boolean;
  onSort?: (fieldName: string) => void;
  currentSortBy?: string;
  currentSortDirection?: 'ASC' | 'DESC';
}

export function ReportTable({
  fields,
  data,
  loading = false,
  onSort,
  currentSortBy,
  currentSortDirection
}: ReportTableProps) {
  const visibleFields = fields.filter(f => f.visible);

  const formatValue = (value: any, field: ReportField): string => {
    if (value === null || value === undefined) return '-';

    switch (field.fieldType) {
      case 'boolean':
        return value ? '✅ Sim' : '❌ Não';
      
      case 'date':
        return new Date(value).toLocaleDateString('pt-BR');
      
      case 'decimal':
        return Number(value).toLocaleString('pt-BR', {
          minimumFractionDigits: 2,
          maximumFractionDigits: 2
        });
      
      case 'number':
        return Number(value).toLocaleString('pt-BR');
      
      default:
        return String(value);
    }
  };

  if (loading) {
    return (
      <div className="flex justify-center items-center py-12">
        <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600"></div>
      </div>
    );
  }

  if (data.length === 0) {
    return (
      <div className="text-center py-12 text-gray-500">
        📭 Nenhum registro encontrado
      </div>
    );
  }

  return (
    <div className="overflow-x-auto">
      <table className="w-full">
        <thead className="bg-gray-50 border-b-2 border-gray-200">
          <tr>
            {visibleFields.map(field => (
              <th
                key={field.name}
                className={`px-4 py-3 text-left text-sm font-semibold text-gray-700 ${
                  field.sortable ? 'cursor-pointer hover:bg-gray-100' : ''
                }`}
                onClick={() => field.sortable && onSort?.(field.name)}
              >
                <div className="flex items-center gap-2">
                  {field.displayName}
                  {field.sortable && currentSortBy === field.name && (
                    <span>
                      {currentSortDirection === 'ASC' ? '▲' : '▼'}
                    </span>
                  )}
                </div>
              </th>
            ))}
          </tr>
        </thead>
        <tbody>
          {data.map((row, idx) => (
            <tr
              key={idx}
              className="border-b hover:bg-gray-50 transition-colors"
            >
              {visibleFields.map(field => (
                <td key={field.name} className="px-4 py-3 text-sm">
                  {formatValue(row[field.name], field)}
                </td>
              ))}
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
```

---

### Componente de Paginação

```tsx
// components/Pagination.tsx

import React from 'react';

interface PaginationProps {
  currentPage: number;
  totalPages: number;
  totalRecords: number;
  pageSize: number;
  onPageChange: (page: number) => void;
  onPageSizeChange?: (pageSize: number) => void;
}

export function Pagination({
  currentPage,
  totalPages,
  totalRecords,
  pageSize,
  onPageChange,
  onPageSizeChange
}: PaginationProps) {
  const startRecord = (currentPage - 1) * pageSize + 1;
  const endRecord = Math.min(currentPage * pageSize, totalRecords);

  const pages = Array.from({ length: totalPages }, (_, i) => i + 1);
  const visiblePages = pages.filter(
    page =>
      page === 1 ||
      page === totalPages ||
      (page >= currentPage - 2 && page <= currentPage + 2)
  );

  return (
    <div className="flex flex-col sm:flex-row justify-between items-center gap-4 mt-4 px-4">
      <div className="text-sm text-gray-600">
        Mostrando {startRecord} a {endRecord} de {totalRecords} registros
      </div>

      <div className="flex items-center gap-2">
        <button
          onClick={() => onPageChange(currentPage - 1)}
          disabled={currentPage === 1}
          className="px-3 py-1 border rounded-md disabled:opacity-50 disabled:cursor-not-allowed hover:bg-gray-50"
        >
          ← Anterior
        </button>

        {visiblePages.map((page, idx) => {
          const prevPage = visiblePages[idx - 1];
          const showEllipsis = prevPage && page - prevPage > 1;

          return (
            <React.Fragment key={page}>
              {showEllipsis && <span className="px-2">...</span>}
              <button
                onClick={() => onPageChange(page)}
                className={`px-3 py-1 border rounded-md ${
                  currentPage === page
                    ? 'bg-blue-600 text-white'
                    : 'hover:bg-gray-50'
                }`}
              >
                {page}
              </button>
            </React.Fragment>
          );
        })}

        <button
          onClick={() => onPageChange(currentPage + 1)}
          disabled={currentPage === totalPages}
          className="px-3 py-1 border rounded-md disabled:opacity-50 disabled:cursor-not-allowed hover:bg-gray-50"
        >
          Próxima →
        </button>
      </div>

      {onPageSizeChange && (
        <select
          value={pageSize}
          onChange={(e) => onPageSizeChange(Number(e.target.value))}
          className="px-3 py-1 border rounded-md"
        >
          <option value={10}>10 por página</option>
          <option value={20}>20 por página</option>
          <option value={50}>50 por página</option>
          <option value={100}>100 por página</option>
        </select>
      )}
    </div>
  );
}
```

---

### Página Completa de Relatório

```tsx
// pages/ReportPage.tsx

import React, { useEffect, useState } from 'react';
import { useParams } from 'react-router-dom';
import { reportService } from '@/services/reportService';
import { ReportFilters } from '@/components/ReportFilters';
import { ReportTable } from '@/components/ReportTable';
import { Pagination } from '@/components/Pagination';
import type { ReportMetadata } from '@/types/report.types';

export function ReportPage() {
  const { reportName } = useParams<{ reportName: string }>();
  
  const [metadata, setMetadata] = useState<ReportMetadata | null>(null);
  const [data, setData] = useState<Record<string, any>[]>([]);
  const [filters, setFilters] = useState<Record<string, string>>({});
  const [loading, setLoading] = useState(false);
  const [exporting, setExporting] = useState(false);
  
  const [pagination, setPagination] = useState({
    page: 1,
    pageSize: 20,
    totalRecords: 0,
    totalPages: 0
  });
  
  const [sort, setSort] = useState<{
    sortBy?: string;
    sortDirection: 'ASC' | 'DESC';
  }>({ sortDirection: 'ASC' });

  // Carregar metadados ao montar
  useEffect(() => {
    if (reportName) {
      loadMetadata();
    }
  }, [reportName]);

  // Executar relatório ao carregar ou quando filtros/paginação mudarem
  useEffect(() => {
    if (metadata) {
      executeReport();
    }
  }, [metadata, pagination.page, pagination.pageSize, sort.sortBy, sort.sortDirection]);

  const loadMetadata = async () => {
    try {
      setLoading(true);
      const result = await reportService.getMetadata(reportName!);
      setMetadata(result);
    } catch (error) {
      console.error('Erro ao carregar metadados:', error);
      alert('Erro ao carregar relatório');
    } finally {
      setLoading(false);
    }
  };

  const executeReport = async () => {
    if (!metadata) return;

    try {
      setLoading(true);
      const result = await reportService.execute(reportName!, {
        filters,
        page: pagination.page,
        pageSize: pagination.pageSize,
        sortBy: sort.sortBy,
        sortDirection: sort.sortDirection
      });

      setData(result.data);
      setPagination(prev => ({
        ...prev,
        totalRecords: result.pagination.totalRecords,
        totalPages: result.pagination.totalPages
      }));
    } catch (error) {
      console.error('Erro ao executar relatório:', error);
      alert('Erro ao executar relatório');
    } finally {
      setLoading(false);
    }
  };

  const handleFilterChange = (name: string, value: string) => {
    setFilters(prev => ({ ...prev, [name]: value }));
  };

  const handleClearFilters = () => {
    setFilters({});
    setPagination(prev => ({ ...prev, page: 1 }));
  };

  const handleSearch = () => {
    setPagination(prev => ({ ...prev, page: 1 }));
    executeReport();
  };

  const handleExport = async () => {
    try {
      setExporting(true);
      await reportService.exportCsv(reportName!, filters);
    } catch (error) {
      console.error('Erro ao exportar:', error);
      alert('Erro ao exportar relatório');
    } finally {
      setExporting(false);
    }
  };

  const handleSort = (fieldName: string) => {
    setSort(prev => ({
      sortBy: fieldName,
      sortDirection:
        prev.sortBy === fieldName && prev.sortDirection === 'ASC'
          ? 'DESC'
          : 'ASC'
    }));
  };

  const handlePageChange = (page: number) => {
    setPagination(prev => ({ ...prev, page }));
  };

  const handlePageSizeChange = (pageSize: number) => {
    setPagination(prev => ({ ...prev, pageSize, page: 1 }));
  };

  if (!metadata) {
    return (
      <div className="flex justify-center items-center min-h-screen">
        <div className="animate-spin rounded-full h-16 w-16 border-b-2 border-blue-600"></div>
      </div>
    );
  }

  return (
    <div className="container mx-auto p-6">
      {/* Header */}
      <div className="flex flex-col md:flex-row justify-between items-start md:items-center mb-6 gap-4">
        <div>
          <h1 className="text-3xl font-bold text-gray-800">
            {metadata.displayName}
          </h1>
          <p className="text-gray-600 mt-1">{metadata.description}</p>
          {metadata.category && (
            <span className="inline-block mt-2 px-3 py-1 bg-blue-100 text-blue-800 text-sm rounded-full">
              {metadata.category}
            </span>
          )}
        </div>
        
        <button
          onClick={handleExport}
          disabled={exporting || loading}
          className="px-6 py-2 bg-green-600 text-white rounded-md hover:bg-green-700 disabled:opacity-50 disabled:cursor-not-allowed flex items-center gap-2"
        >
          {exporting ? '⏳ Exportando...' : '📥 Exportar CSV'}
        </button>
      </div>

      {/* Filtros */}
      {metadata.filters.length > 0 && (
        <div className="mb-6">
          <ReportFilters
            filters={metadata.filters}
            values={filters}
            onChange={handleFilterChange}
            onClear={handleClearFilters}
            onSearch={handleSearch}
            loading={loading}
          />
        </div>
      )}

      {/* Tabela */}
      <div className="bg-white rounded-lg shadow overflow-hidden">
        <ReportTable
          fields={metadata.fields}
          data={data}
          loading={loading}
          onSort={handleSort}
          currentSortBy={sort.sortBy}
          currentSortDirection={sort.sortDirection}
        />

        {/* Paginação */}
        {pagination.totalRecords > 0 && (
          <Pagination
            currentPage={pagination.page}
            totalPages={pagination.totalPages}
            totalRecords={pagination.totalRecords}
            pageSize={pagination.pageSize}
            onPageChange={handlePageChange}
            onPageSizeChange={handlePageSizeChange}
          />
        )}
      </div>
    </div>
  );
}
```

---

## 🚀 Como Usar

### 1. Adicionar Rota
```tsx
// routes.tsx ou App.tsx
import { ReportPage } from '@/pages/ReportPage';

<Route path="/reports/:reportName" element={<ReportPage />} />
```

### 2. Adicionar ao Menu
```tsx
<MenuItem to="/reports/products_list" icon="📊">
  Relatório de Produtos
</MenuItem>
```

### 3. Ou criar uma página de listagem de relatórios
```tsx
// pages/ReportsListPage.tsx
export function ReportsListPage() {
  const [reports, setReports] = useState<Report[]>([]);

  useEffect(() => {
    reportService.listReports().then(data => {
      setReports(data.reports);
    });
  }, []);

  return (
    <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
      {reports.map(report => (
        <Link
          key={report.name}
          to={`/reports/${report.name}`}
          className="p-6 bg-white rounded-lg shadow hover:shadow-lg transition-shadow"
        >
          <h3 className="text-xl font-bold mb-2">{report.displayName}</h3>
          <p className="text-gray-600 text-sm">{report.description}</p>
          <span className="inline-block mt-3 text-blue-600 text-sm">
            Ver relatório →
          </span>
        </Link>
      ))}
    </div>
  );
}
```

---

## ✅ Benefícios

- ✅ **Sem código específico**: Mesmo componente para todos os relatórios
- ✅ **Configurável via banco**: Novos relatórios sem deploy
- ✅ **Filtros dinâmicos**: Renderiza filtros baseado em metadados
- ✅ **Export integrado**: CSV com um clique
- ✅ **Type-safe**: Interfaces TypeScript completas
- ✅ **Paginação automática**: Backend controla a paginação
- ✅ **Ordenação**: Clique no header da coluna para ordenar
- ✅ **Responsivo**: Funciona em desktop e mobile

---

## 📝 Relatórios Disponíveis

### products_list - Relatório de Produtos
- **Categoria:** Produtos
- **Descrição:** Lista completa de produtos cadastrados
- **Campos:** 7 (código, barcode, descrição, unidade, marca, grupo, ativo)
- **Filtros:** 3 (status, descrição, código)

---

## 💡 Como Adicionar Novos Relatórios

Para adicionar um novo relatório, basta inserir dados nas seguintes tabelas no banco de dados:

1. **reports** - Configuração geral do relatório
2. **report_fields** - Campos/colunas que serão exibidos
3. **report_filters** - Filtros disponíveis
4. **report_filter_options** - Opções para filtros do tipo select

O frontend renderizará automaticamente o novo relatório sem necessidade de alteração de código!

---

## 🔧 Troubleshooting

### Erro 401 (Unauthorized)
- Verifique se o token está sendo enviado no header `Authorization: Bearer {token}`
- Verifique se o token não expirou

### Erro 404 (Report not found)
- Verifique se o `reportName` está correto
- Verifique se o relatório está ativo no banco de dados

### CSV não baixa
- Verifique se o navegador não está bloqueando downloads automáticos
- Verifique se `responseType: 'blob'` está configurado no axios

### Filtros não funcionam
- Verifique se os valores estão sendo enviados como string
- Para filtros boolean, envie `"true"` ou `"false"` (string, não boolean)

---

**Documentação gerada em:** 2025-12-03  
**Versão da API:** 1.0  
**Backend:** ASP.NET Core 9.0  
**Endpoint Base:** http://localhost:5287
