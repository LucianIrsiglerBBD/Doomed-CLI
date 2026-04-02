package com.bbdgrads.apiserver.model;

import java.time.LocalDateTime;

import jakarta.persistence.Column;
import jakarta.persistence.Entity;
import jakarta.persistence.GeneratedValue;
import jakarta.persistence.GenerationType;
import jakarta.persistence.Id;
import jakarta.persistence.PrePersist;
import jakarta.persistence.Table;
import lombok.Data;

@Entity
@Table(name = "\"Maps\"")
@Data
public class GameMap {
    @Id
    @GeneratedValue(strategy = GenerationType.AUTO)
    private Integer id;

    @Column(unique = true, nullable = false)
    private String name;

    @Column(nullable = false)
    private String data;

    @Column(nullable = false, updatable = false, name="created_at")
    private LocalDateTime createdAt;

    @PrePersist
        protected void onCreate() {
            this.createdAt = LocalDateTime.now();
        }

}
