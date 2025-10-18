# Introduction

## Why Test APIs with Swagger?

In modern software development, APIs are the backbone of communication between services. But a well-documented API isn’t enough—you need to ensure it behaves exactly as promised.

Swagger (now part of the OpenAPI Specification) goes beyond documentation. By defining a precise contract for your API, Swagger enables automated, contract-based testing that catches regressions, enforces consistency, and validates responses against expected schemas—before bugs reach production.

Instead of writing manual test scripts for every endpoint, you can leverage your OpenAPI spec as a single source of truth for both documentation and validation. This approach saves time, reduces errors, and aligns frontend, backend, and QA teams around a shared definition of correctness.

In this article, we’ll walk through a real-world example using Swagger and the Dredd testing tool to automatically verify an API—end to end, with real code.

## What Is Swagger/OpenAPI?

Swagger is a set of open-source tools built around the OpenAPI Specification (OAS), an industry-standard format for describing RESTful APIs. Originally created by SmartBear, the term "Swagger" is often used interchangeably with OpenAPI, though technically:

- **OpenAPI** is the specification (a YAML or JSON file that defines your API’s endpoints, parameters, request/response formats, and more).
- **Swagger** refers to the tooling ecosystem (like Swagger UI, Swagger Editor, and Swagger Codegen) that helps you design, visualize, and interact with APIs based on that spec.

An OpenAPI document acts as a contract between your API and its consumers. For example, it can specify that a `GET /products` endpoint returns a 200 status code with an array of objects, each containing `id`, `name`, and `price` fields of specific types.

Because this contract is machine-readable, it enables powerful automation:
- Interactive documentation (via Swagger UI)
- Client and server code generation
- Automated API testing — which is exactly what we’ll focus on next.

## Real-World Example: E-commerce Product API

Imagine you’re part of a team building a backend for an e-commerce platform. Your API handles core product operations:

- `GET /products` → returns a list of all products
- `GET /products/{id}` → fetches a single product by ID
- `POST /products` → creates a new product

Your frontend team relies on consistent responses—like price always being a number, not a string. A small schema drift could break the checkout page.

Instead of hoping everything works (or writing dozens of manual test cases), you define the expected behavior upfront using an OpenAPI spec. This spec becomes both your documentation and your test blueprint.

In the next section, we’ll write that spec—and then use it to automatically verify your live API behaves exactly as promised.

## Step 1: Write the OpenAPI Specification

We’ll define a minimal but realistic OpenAPI 3.0 spec for our e-commerce product API. Save this as `openapi.yaml`:

```yaml
openapi: 3.0.3
info:
  title: E-commerce Product API
  version: 1.0.0
servers:
  - url: http://localhost:3000
paths:
  /products:
    get:
      summary: List all products
      responses:
        '200':
          description: A list of products
          content:
            application/json:
              schema:
                type: array
                items:
                  $ref: '#/components/schemas/Product'
    post:
      summary: Create a new product
      requestBody:
        required: true
        content:
          application/json:
            schema:
              $ref: '#/components/schemas/ProductInput'
      responses:
        '201':
          description: Product created
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/Product'

components:
  schemas:
    Product:
      type: object
      properties:
        id:
          type: integer
        name:
          type: string
        price:
          type: number
          format: float
      required: [id, name, price]

    ProductInput:
      type: object
      properties:
        name:
          type: string
        price:
          type: number
          format: float
      required: [name, price]
```

This spec clearly defines:
- Expected request/response formats
- Required fields and data types
- HTTP status codes for success cases

## Step 2: Test the API with Dredd

Dredd is a command-line tool that validates your live API against your OpenAPI (or API Blueprint) specification. It sends real HTTP requests and checks responses for:

- Correct status codes
- Proper headers
- Schema compliance (structure, types, required fields)

### Install Dredd

```bash
npm install -g dredd
```

### Run the Tests

Assuming your API runs locally on http://localhost:3000, execute:

```bash
dredd openapi.yaml http://localhost:3000
```

Dredd will:
- Parse your openapi.yaml
- Hit GET /products and POST /products
- Verify that responses match the defined schemas

- If your API returns `"price": "19.99"` (a string), Dredd fails—because the spec requires a number.
- If POST /products returns 200 instead of 201, Dredd fails—status code mismatch.

This gives you instant feedback on contract violations—no manual inspection needed.

> Tip: Use `--hookfiles=hooks.js` to inject test data (e.g., create a product before testing GET /products/{id}).

In the next step, we’ll automate this in CI—so every pull request is validated against your API contract.

## Step 3: Automate Tests in CI/CD (GitHub Actions)

Running API contract tests manually isn’t scalable. Instead, integrate them into your CI pipeline so every code change is automatically validated against your OpenAPI spec.

Here’s a `.github/workflows/api-tests.yml` workflow that:
- Starts your API server
- Runs Dredd against it

```yaml
name: API Contract Tests
on: [push, pull_request]
jobs:
  test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - name: Setup Node.js
        uses: actions/setup-node@v4
        with:
          node-version: '18'

      - name: Install dependencies
        run: npm ci

      - name: Start API server
        run: npm start &
        # Give the server a moment to boot
        env:
          PORT: 3000

      - name: Wait for server to be ready
        run: |
          while ! curl -s http://localhost:3000/health; do
            sleep 1
          done

      - name: Install Dredd
        run: npm install -g dredd

      - name: Run contract tests
        run: dredd openapi.yaml http://localhost:3000
```

> Note: Make sure your app has a lightweight health check endpoint (e.g., GET /health) so the workflow knows when the server is ready.

With this in place, any PR that breaks the API contract—like changing a response field or returning the wrong status code—will fail the build immediately, preventing regressions before they reach staging or production.

## Key Benefits of Contract-Based Testing

Using your OpenAPI spec as a test contract delivers more than just validation—it transforms how teams build and maintain APIs:

- **Single Source of Truth**: Your spec defines behavior for documentation, testing, and code generation—eliminating drift between what’s documented and what’s implemented.
- **Early Bug Detection**: Schema mismatches, wrong status codes, or missing fields are caught in CI—long before they reach users.
- **Faster Frontend Development**: Frontend teams can mock APIs or generate SDKs from the spec and trust that the backend will conform—no more guessing or breaking changes.
- **Reduced Manual Testing**: No need to write repetitive test cases for every endpoint. The contract is the test.
- **Safer Refactoring**: Refactor your API logic with confidence: if the contract still passes, you haven’t broken compatibility.
- **Better Collaboration**: Product, QA, backend, and frontend teams all align around the same API definition—reducing miscommunication and rework.

In short: contract-based testing turns your API spec from a passive document into an active safety net.

## Conclusion & Next Steps

Swagger (OpenAPI) is far more than a documentation tool—it’s a foundation for reliable, testable, and maintainable APIs. By treating your API spec as a living contract and validating it automatically with tools like Dredd, you catch bugs early, reduce integration friction, and ship with confidence.

You’ve now seen how to:
- Define a real-world OpenAPI spec
- Test a live API against it
- Automate validation in CI/CD

What’s next?
- Try Schemathesis for property-based testing that generates hundreds of edge-case requests from your spec.
- Use Swagger UI to explore and manually test your API in the browser.
- Generate client SDKs automatically with OpenAPI Generator for frontend or mobile apps.

Start small: add an OpenAPI spec to one service, run Dredd in CI, and watch your API quality rise—without writing extra test code.

Happy testing! 🚀
