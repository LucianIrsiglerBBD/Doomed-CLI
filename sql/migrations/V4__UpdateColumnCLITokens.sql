ALTER TABLE "cli_tokens"
DROP COLUMN email;

ALTER TABLE "cli_tokens"
ADD COLUMN user_id int NOT NULL DEFAULT 0;

ALTER TABLE "cli_tokens"
ADD CONSTRAINT fk_user_id 
FOREIGN KEY (user_id) REFERENCES "Users"(id);
