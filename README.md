# A-more-beautifull-Document-Ingelligence ✨📄

This repository contains an Azure Functions app that reformats Azure Document Intelligence responses into cleaner, business-friendly JSON.

## Why this exists 🎯

Raw Document Intelligence output is powerful, but it can be noisy for downstream processing. It often includes lots of layout/geometry metadata (for example: `boundingRegions`, polygons, spans, and related coordinates).

This project trims the noise and keeps the useful parts (content, headers, rows, invoice fields), so your APIs and apps can consume results more easily. Less metadata soup, more usable data 🍲➡️📦

## Disclaimer ⚠️

- The extraction logic in this project was shaped using a limited set of customer documents.
- Invoice formats vary a lot, so this may not work perfectly for all invoices or all OCR/read results.
- Think of this as a practical baseline: use it, test it, and adapt it to your own files.
- Vibe coding can be super helpful for iterating quickly and tuning the rules to your document set. ✨

## What the functions do 🛠️

### `BeautifulTables` 📊
- Accepts a JSON array of Document Intelligence table objects.
- Normalizes tables into a generic schema.
- Detects and outputs either:
  - `key-value` tables (label → value)
  - `columnar` tables (headers + rows)
- Preserves semantic table meaning while removing low-value OCR layout noise.

### `BeautifulContent` 📄
- Accepts a full Document Intelligence analyze-result payload.
- Returns clean page and paragraph content.
- Requires JSON in the request body.

### `BeautifulEntries` 🧾
- Accepts paragraph-based JSON (or analyze-result payload containing paragraphs).
- Extracts structured invoice line entries (position, quantity, unit, VAT, prices, totals, etc.).
- Requires JSON in the request body.

## Project layout 🗂️

- `azfnct/` – Azure Functions project
  - `Functions/` – HTTP-triggered function endpoints
  - `Services/` – Transformation and extraction logic
  - `Models/` – DTOs used by functions/services
- `infra/` – Infrastructure/deployment scripts (`main.bicep`, `main.bicepparam`, `deploy.ps1`)
- `test-beautifultables.ps1` – Example script with a large sample payload for `BeautifulTables`

## Prerequisites ✅

- .NET SDK 10.0
- Azure Functions Core Tools v4

## Run locally 🚀

From the repo root:

```powershell
cd azfnct
dotnet build
cd bin/Debug/net10.0
func host start
```

The host starts on `http://localhost:7071` by default.

## Endpoints 🌐

- `POST /api/BeautifulTables`
- `POST /api/BeautifulContent`
- `POST /api/BeautifulEntries`

## Quick usage examples ⚡

### `BeautifulContent` using request body JSON

```powershell
$payload = Get-Content "C:\path\to\analyze-result.json" -Raw
Invoke-RestMethod \
  -Method Post \
  -Uri "http://localhost:7071/api/BeautifulContent" \
  -ContentType "application/json" \
  -Body $payload
```

### `BeautifulEntries` using request body JSON

```powershell
$payload = Get-Content "C:\path\to\paragraphs.json" -Raw
Invoke-RestMethod \
  -Method Post \
  -Uri "http://localhost:7071/api/BeautifulEntries" \
  -ContentType "application/json" \
  -Body $payload
```

### `BeautifulTables` using request body JSON

```powershell
$tables = Get-Content "C:\path\to\tables.json" -Raw
Invoke-RestMethod \
  -Method Post \
  -Uri "http://localhost:7071/api/BeautifulTables" \
  -ContentType "application/json" \
  -Body $tables
```

## Notes 📝

- Functions use `AuthorizationLevel.Function`. Depending on your environment, a function key may be required.
- This project focuses on output clarity and downstream usability by reducing response noise from OCR/layout metadata.
