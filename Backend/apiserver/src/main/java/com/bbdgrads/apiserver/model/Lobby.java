package com.bbdgrads.apiserver.model;

import java.time.LocalDateTime;
import jakarta.persistence.Column;
import jakarta.persistence.Entity;
import jakarta.persistence.GeneratedValue;
import jakarta.persistence.GenerationType;
import jakarta.persistence.Id;
import jakarta.persistence.JoinColumn;
import jakarta.persistence.ManyToOne;
import jakarta.persistence.PrePersist;
import jakarta.persistence.Table;
import lombok.AllArgsConstructor;
import lombok.Data;
import lombok.NoArgsConstructor;

@Entity
@Data
@AllArgsConstructor
@NoArgsConstructor
@Table(name = "\"Lobbies\"")
public class Lobby {
    @Id
    @GeneratedValue(strategy = GenerationType.AUTO)
    private Integer id;
    
    @Column(unique = true,nullable = false)
    private String code;

     @Column(nullable = false,name="is_started")
    private boolean started = false;
    
    @ManyToOne(optional = false)
    @JoinColumn(name = "host_user_id", nullable = false)
    private User hostUser;

    @ManyToOne(optional = false)
    @JoinColumn(name = "map_id", nullable = false)
    private GameMap map;

    @Column(nullable = true,name="start_time")
    private LocalDateTime startTime;

    @Column(nullable = true,name="end_time")
    private LocalDateTime endTime;
    
     @Column(name="created_at")
    private LocalDateTime createdAt;

    @PrePersist
    protected void onCreate() {
        this.createdAt = LocalDateTime.now();
        
    }
    

}
