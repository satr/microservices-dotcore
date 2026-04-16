-- Initialises separate databases for each microservice.
-- This script runs once when the postgres container is first created.
CREATE DATABASE users_db;
CREATE DATABASE books_db;
CREATE DATABASE booking_db;

