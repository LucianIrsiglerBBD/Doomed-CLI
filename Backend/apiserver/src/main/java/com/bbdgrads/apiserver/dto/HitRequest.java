package com.bbdgrads.apiserver.dto;

import lombok.AllArgsConstructor;
import lombok.Data;
import lombok.NoArgsConstructor;

@Data
@NoArgsConstructor
@AllArgsConstructor
public class HitRequest {
    private String shooter;
    private String target;
    private String weaponName;
    private int damage;
}
