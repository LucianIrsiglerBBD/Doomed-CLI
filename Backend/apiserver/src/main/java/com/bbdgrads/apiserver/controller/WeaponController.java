package com.bbdgrads.apiserver.controller;

import java.util.List;
import java.util.Optional;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.DeleteMapping;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PathVariable;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.RequestBody;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;
import com.bbdgrads.apiserver.model.Weapon;
import com.bbdgrads.apiserver.services.WeaponService;
import lombok.RequiredArgsConstructor;

@RestController
@RequestMapping("api/weapons")
@RequiredArgsConstructor
public class WeaponController {

    private final WeaponService weaponService;

    @GetMapping
    public ResponseEntity<List<Weapon>> getWeapons() {
        return new ResponseEntity<List<Weapon>>(weaponService.getWeapons(), HttpStatus.OK);
    }

    @GetMapping("/{weaponId}")
    public ResponseEntity<Weapon> getWeaponById(@PathVariable int weaponId) {
        return ResponseEntity.ok(weaponService.getWeaponById(weaponId));
    }

    @PostMapping
    public ResponseEntity<Weapon> createWeapon(@RequestBody Weapon weapon) {
        return new ResponseEntity<>(weaponService.createWeapon(weapon), HttpStatus.CREATED);
    }

    @DeleteMapping("/{weaponId}")
    public ResponseEntity<Void> deleteWeapon(@PathVariable int weaponId) {
        weaponService.deleteWeapon(weaponId);
        return ResponseEntity.noContent().build();
    }

}
