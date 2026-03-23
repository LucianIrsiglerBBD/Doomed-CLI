# Springboot API server

## Prerequisites
- [Java SE Development Kit 26](https://www.oracle.com/africa/java/technologies/downloads/#jdk26-windows)

## How to run locally
1. Set the following environment variables to point to your local postgresql instance
- DB_PASSWORD
- DB_USERNAME
- DB_URI

2. Run the following commands
```
cd apiserver
mvn spring-boot:run
```

# Server CLI
## How to run locally
1.Run the following commands
```
cd server_cli
mvn exec:java
```