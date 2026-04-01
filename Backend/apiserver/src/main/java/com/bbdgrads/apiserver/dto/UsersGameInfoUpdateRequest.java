package com.bbdgrads.apiserver.dto;

import lombok.Data;

@Data
public class UsersGameInfoUpdateRequest {

    private int userId;
    private int lobbyId;
    private int speed;
    private int health;
    private int x;
    private int y;

}
