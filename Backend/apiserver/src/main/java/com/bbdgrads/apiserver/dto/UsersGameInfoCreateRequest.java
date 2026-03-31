package com.bbdgrads.apiserver.dto;

import lombok.Data;

@Data
public class UsersGameInfoCreateRequest {

    private int userId;
    private int lobbyId;
    private int speed;
    private int x;
    private int y;

}
