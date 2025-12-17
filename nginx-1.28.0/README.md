\# Nginx Reverse Proxy



This configuration sets up Nginx as a \*\*reverse proxy\*\* for the project, forwarding requests from port 8080 to the frontend and backend.



---



\## Features

\- Frontend (`/`) → localhost:3000  

\- Backend API (`/api/`) → localhost:5298  

\- WebSocket support (`/hubs/`) for SignalR  

\- Swagger UI (`/swagger/`)  

\- Health check (`/health`)  

\- Security headers and 10 MB upload limit  

\- Denies access to hidden files (except `.well-known`)  



---



\## Usage



sudo nginx       # Linux

nginx            # Windows



\#Access the project at http://localhost:8080/.

