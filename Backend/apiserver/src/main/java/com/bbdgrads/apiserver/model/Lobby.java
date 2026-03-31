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
@Data
@AllArgsConstructor
@NoArgsConstructor
@Table(name = "Lobbies")
public class Lobby {
    @Id
    @GeneratedValue(strategy = GenerationType.AUTO)
    private Integer Id;
    
    @Column(unique = true,nullable = false)
    private String code;

     @Column(nullable = false)
    private boolean isStarted = false;
    
    @ManyToOne(optional = false)
    @JoinColumn(name = "hostUserId", nullable = false)
    private User hostUser;

    @ManyToOne(optional = false)
    @JoinColumn(name = "mapId", nullable = false)
    private GameMap map;

    @Column(nullable = true)
    private LocalDateTime startTime;

    @Column(nullable = true)
    private LocalDateTime endTime;
    private LocalDateTime createdAt = LocalDateTime.now();

    

}
