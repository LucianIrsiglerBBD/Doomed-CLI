package com.bbdgrads.apiserver.services;

import java.util.List;

import org.springframework.stereotype.Service;

import com.bbdgrads.apiserver.errors.DuplicateResourceException;
import com.bbdgrads.apiserver.errors.WeaponNotFoundException;
import com.bbdgrads.apiserver.model.Weapon;
import com.bbdgrads.apiserver.repository.WeaponRepository;

import jakarta.transaction.Transactional;
import lombok.RequiredArgsConstructor;

@Service
@RequiredArgsConstructor
@Transactional
public class WeaponService {

    private final WeaponRepository weaponRepository;

    public List<Weapon> getWeapons() {
        return weaponRepository.findAll();
    }

    public Weapon getWeaponById(int weaponId) {
        return weaponRepository.findById(weaponId)
            .orElseThrow(() -> new WeaponNotFoundException("Weapon with id " + weaponId + " not found"));
    }

    public Weapon createWeapon(Weapon newWeapon) {

        Weapon existingByName = weaponRepository.findByName(newWeapon.getName());
        if (existingByName != null) {
            throw new DuplicateResourceException(
                "Weapon with name '" + newWeapon.getName() + "' already exists");
        }

        return weaponRepository.save(newWeapon);
    }

    public Weapon damageWeapon(int weaponId) {

        Weapon weapon = weaponRepository.findById(weaponId)
            .orElseThrow(() -> new WeaponNotFoundException(
                    "Weapon with id " + weaponId + " not found"));

        int currentDamage = weapon.getDamage();
        if (currentDamage > 0) {
            weapon.setDamage(currentDamage - 1);
        }

        return weaponRepository.save(weapon);
    }

    public void deleteWeapon(int weaponId) {
        Weapon weapon = weaponRepository.findById(weaponId)
            .orElseThrow(() -> new WeaponNotFoundException(
                    "Weapon with id " + weaponId + " not found"));

        weaponRepository.delete(weapon);
    }
}