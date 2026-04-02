CREATE TABLE cli_tokens (
    token TEXT PRIMARY KEY,
    email TEXT NOT NULL,
    created_at TIMESTAMP NOT NULL,
    expires_at TIMESTAMP NOT NULL
);