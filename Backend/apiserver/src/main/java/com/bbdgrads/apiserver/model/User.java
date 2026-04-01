package com.bbdgrads.apiserver.model;

import java.time.LocalDateTime;

import jakarta.persistence.Column;
import jakarta.persistence.Entity;
import jakarta.persistence.GeneratedValue;
import jakarta.persistence.GenerationType;
import jakarta.persistence.Id;
import jakarta.persistence.JoinColumn;
import jakarta.persistence.ManyToOne;
import jakarta.persistence.Table;
import lombok.AllArgsConstructor;
import lombok.Data;
import lombok.NoArgsConstructor;

@Entity
@AllArgsConstructor
@NoArgsConstructor
@Data
@Table(name = "\"Users\"")
public class User {
    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private Integer id;

    @Column(nullable = false, unique = true)
    private String name;
    
    @Column(nullable = false, unique = true)
    private String email;
   
    @ManyToOne
    @JoinColumn(name = "lobby_id")
    private Lobby lobby;

     @Column(nullable = false, updatable = false,name="created_at")
    private LocalDateTime createdAt = LocalDateTime.now();

    public User(String name, String email) {
        this.name = name;
        this.email = email;
    }

}