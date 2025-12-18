LMS (Library Management System) – Project Documentation
Overview

LMS is a modern library management system built using .NET 9 Web API for the backend and Next.js for the frontend. It provides role-based access for users and admins, real-time notifications, AI-powered summaries,personal insights and user habits, and recommendations. The system is fully containerized using Docker and orchestrated with Docker Compose. It follows Clean Architecture with separation of concerns across API, Application, Infrastructure, and Domain layers.

Tech stack

Backend: .NET 9 Web API, C#
Frontend: Next.js, React
Database: MSSQL for main data, Redis for caching and rate limiting
Real-time communication: SignalR
AI / LLM: OpenAI API for AI query agent, recommendations, and insights
Containerization: Docker, Docker Compose
Reverse Proxy: Nginx
Testing: xUnit, Moq

Architecture & Infrastructure

The project follows Clean Architecture with layered separation:

API Layer: ASP.NET Core Web API exposing endpoints for frontend consumption.
Application Layer: Business logic, services, DTOs, and validation.
Domain Layer: Core entities and domain logic.
Infrastructure Layer: Database context (MSSQL), caching (Redis), SignalR hubs, OpenAI integration, and repositories.

Docker & Nginx Orchestration

The system is orchestrated using Docker Compose
Reverse Proxy: Nginx acts as the single entry point (port 8081), routing traffic to the Frontend, API, and WebSockets.
WebSocket Optimization: Nginx is specifically configured with Upgrade and Connection headers to support SignalR persistent connections.
Service Isolation: The Database and Redis instances are kept off the public internet, accessible only by the Backend within the Docker bridge network.

Entities

User

Implemented via ASP.NET Identity (ApplicationUser)
Properties: FirstName, LastName, FullName, Books, RefreshTokens, etc.
Relationships: One-to-many with Book.

Book

Properties: Title, Author, Genre, Price, ReadingStatus(enum), UserId, Rating, PublicationYear, CoverImageUrl.
Relationships: Each book belongs to one user; a user can have many books.

Features

Authentication & Authorization

JWT Bearer Tokens with access & refresh tokens.
Role-based authorization: User and Admin.
Login endpoint rate-limited for security.

User Features

Register, login, and manage profile.
View, create, update, delete personal books.
Search personal books.
View personal insights and reading habits.
Receive real-time notifications via SignalR when admins modify user roles or remove users.
Receive AI-powered book recommendations based on reading history; can save or dismiss recommendations.

Admin Features

View and manage all users and books.
Promote users to admin or change roles.
Access library-wide  insights.
Generate habits for any user.

Real-Time Engagement

SignalR Hubs: Instant notifications for administrative actions (role changes, account updates).
Toast Notifications: Real-time UI feedback in the Next.js frontend without page refreshes.

AI Integration

AI Query Agent: Users and admins can ask questions (via OpenAI API).
Uses caching to reduce repeated AI calls and improves performance.
Built-in rate limiter (Redis) prevents excessive AI requests.

Recommendations Engine

Suggests books to users based on reading patterns.
Suggestions are AI-generated.
Users can save or dismiss recommendations.

Insights Panel

AI-generated summaries of the library or user reading habits.
Only the summary text comes from AI; statistics and habits are calculated from app data.

Testing

Unit tests for core services and validators.
Integration tests for key API endpoints.

 API Documentation

###  Postman Collection
For easier API testing, you can import  pre-configured Postman collection:

* **[Download LMS API Collection](https://raw.githubusercontent.com/lorik-aliuu/LMS/66b7ffa3e16f5d276c4eeeadef241ed92a8bc945/postman-collection/LMSAPI.postman_collection.json)**

Setup & Installation

Prerequisites
Docker Desktop installed.
An OpenAI API Key (for AI features).

Steps to Run

1.Clone the Repo

2.Update your .env files with your OpenAI_ApiKey,ConnectionStrings, JWT, default admin credentials

3.Launch via Docker: docker-compose up -d --build

4.Access Points:

Main Application: http://localhost:8081

API Documentation (Swagger): http://localhost:5298/swagger

Redis Insight: http://localhost:8001

Future Roadmap
CI/CD Pipeline: Implement GitHub Actions to automatically run unit tests and build Docker images on every push to ensure code stability.
Unit Test Coverage: Increase test coverage for the Infrastructure layer and add Integration Tests for the SignalR notification flow.
Advanced Analytics Integration with Grafana and Prometheus to monitor API performance and Redis cache hit rates.


