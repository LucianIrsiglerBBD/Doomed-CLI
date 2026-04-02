package com.bbdgrads.apiserver.model;

import java.time.LocalDateTime;

import jakarta.persistence.Column;
import jakarta.persistence.Entity;
import jakarta.persistence.GeneratedValue;
import jakarta.persistence.GenerationType;
import jakarta.persistence.Id;
import jakarta.persistence.Table;
import lombok.Data;

@Entity
@Table(name = "cli_tokens")
@Data
public class CliToken {

    @Id
    @GeneratedValue(strategy = GenerationType.UUID)
    private String token;
    
    @Column(name = "user_id", nullable = false)
    private int userId;
    private LocalDateTime createdAt;
    private LocalDateTime expiresAt;
    public boolean isExpired() {
        return LocalDateTime.now().isAfter(expiresAt);
    }
}