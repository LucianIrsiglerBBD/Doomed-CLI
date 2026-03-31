package com.bbdgrads.apiserver.model;

import java.time.LocalDateTime;
import java.util.ArrayList;
import java.util.List;

import jakarta.persistence.Column;
import jakarta.persistence.Entity;
import jakarta.persistence.GeneratedValue;
import jakarta.persistence.GenerationType;
import jakarta.persistence.Id;
import jakarta.persistence.JoinColumn;
import jakarta.persistence.OneToOne;
import jakarta.persistence.Table;
import lombok.AllArgsConstructor;
import lombok.Data;
import lombok.NoArgsConstructor;

@Entity
@AllArgsConstructor
@NoArgsConstructor
@Data
@Table(name= "Users")
public class User
{
    @Id
    @GeneratedValue(strategy =  GenerationType.AUTO)
    private Integer id;
    
    @Column(nullable = false,unique = true)
    private String name;
    
    @Column(nullable = false)
    private String passwordHash;
   
    @OneToOne
    @JoinColumn(name = "lobbyId", unique = true)
    private Lobby lobby;

    @OneToOne
    @JoinColumn(name = "weaponId", unique = true)
    private  Weapon weapon;

     @Column(nullable = false, updatable = false)
    private LocalDateTime createdAt = LocalDateTime.now();

}