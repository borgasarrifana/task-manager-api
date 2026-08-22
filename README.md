# Task Manager API

A RESTful task management API built with ASP.NET Core, demonstrating a layered architecture, JWT authentication, and a relational data model with EF Core and PostgreSQL.

## Overview

This project models Projects and Tasks, where each Project can contain many Tasks. It was built as a hands-on learning project to practice core backend patterns expected in professional ASP.NET Core development: clean separation of concerns, secure authentication, and proper REST API design.

## Tech Stack

- **ASP.NET Core (.NET 10)** — Controller-based Web API
- **Entity Framework Core** — ORM, code-first migrations
- **PostgreSQL** (hosted on Supabase) — relational database
- **JWT Bearer Authentication** — token-based auth
- **BCrypt.Net** — secure password hashing
- **Swagger / OpenAPI** — interactive API documentation

## Architecture

The project follows a layered architecture to separate concerns: