# DICOM Service API

A microservice for uploading DICOM files, querying DICOM header tags, and rendering PNG images of DICOM frames.

---

## Table of Contents

1. [Prerequisites](#prerequisites)
2. [Clone & Build](#clone--build)
3. [Run Locally (CLI)](#run-locally-cli)
4. [API Endpoints](#api-endpoints)
5. [Quick Testing](#quick-testing)
6. [Running Unit Tests](#running-unit-tests)
7. [Cleaning Up](#cleaning-up)

---

## Prerequisites

* [.NET 8 SDK](https://dotnet.microsoft.com/download)

---

## Clone & Build

```bash
# Clone the repository
git clone git@github.com:VangjushZ/DicomService.git

# Navigate into the solution root and then into the API project
cd DicomService.API
```

### Restore & Build

```bash
# Restore NuGet packages and project dependencies
dotnet restore

# Compile the project and its dependencies
dotnet build
```

---

## Run Locally (CLI)

This will auto-apply EF Core migrations, create the SQLite database (`app.db`), and start the service.

```bash
cd DicomService.API

# Run in HTTP-only mode (default profile)
dotnet run --launch-profile http

# Or run with HTTPS enabled
dotnet run --launch-profile https
```

* **HTTP** endpoint:  [http://localhost:5196](http://localhost:5196)
* **HTTPS** endpoint: [https://localhost:7016](https://localhost:7016)

You can adjust these URLs in `Properties/launchSettings.json` under the respective profiles.

Once the service is running, open the Swagger UI to explore and test all endpoints:

* **Swagger (HTTP)**:  [http://localhost:5196/swagger](http://localhost:5196/swagger)
* **Swagger (HTTPS)**: [https://localhost:7016/swagger](https://localhost:7016/swagger)

---

## API Endpoints

| Method   | Path                           | Description                                                |
| -------- | ------------------------------ | ---------------------------------------------------------- |
| **GET**  | `/api/dicom`                   | List all uploaded DICOM files (ID, filename, upload time). |
| **POST** | `/api/dicom/upload`            | Upload a DICOM file (`multipart/form-data`, field `file`). |
| **GET**  | `/api/dicom/{id}/header?tag=`  | Retrieve a header tag value (e.g., `?tag=0002,0000`).      |
| **GET**  | `/api/dicom/{id}/image?frame=` | Render a frame as PNG (`frame` defaults to `0`).           |

---

## Quick Testing

You can test endpoints via **Swagger UI** or using command-line tools:

* **Swagger UI**: open [http://localhost:5196/swagger](http://localhost:5196/swagger) or [https://localhost:7016/swagger](https://localhost:7016/swagger) in your browser and interactively try each endpoint.

### Upload a file

```bash
curl -X POST http://localhost:5196/api/dicom/upload \
  -F "file=@/path/to/example.dcm"
```

**Sample response**:

```json
{
  "id": "0160854b-46ae-49b6-8084-876fbf45f427",
  "fileName": "example.dcm",
  "filePath": "0160854b-46ae-49b6-8084-876fbf45f427-example.dcm"
}
```

### Fetch a header

```bash
curl "http://localhost:5196/api/dicom/{id}/header?tag=0002,0000"
```

### Render a PNG

```bash
curl "http://localhost:5196/api/dicom/{id}/image?frame=0" --output frame0.png
```

---

## Running Unit Tests

Navigate into the test project directory and run the tests:

```bash
cd DicomService.Tests

dotnet test
```

---

## Cleaning Up

* **Stop** the service: `Ctrl+C`
* **Remove** generated files:

  ```bash
  rm DicomService.API/app.db
  rm -rf DicomService.API/dicom-uploads
  ```
