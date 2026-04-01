package com.bbdgrads.apiserver.dto;

import lombok.AllArgsConstructor;
import lombok.Data;
import lombok.NoArgsConstructor;

@Data
@NoArgsConstructor
@AllArgsConstructor
public class CreateLobbyRequest {
    private String username;
    private int mapId;
    private int durationInMinutes = 30;   
}
