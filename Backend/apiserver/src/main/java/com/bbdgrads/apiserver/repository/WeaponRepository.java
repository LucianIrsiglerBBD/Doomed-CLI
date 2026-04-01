package com.bbdgrads.apiserver.repository;

import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.stereotype.Repository;
import com.bbdgrads.apiserver.model.Weapon;
@Repository
public interface WeaponRepository extends JpaRepository<Weapon, Integer> {
    
    Weapon findByName(String name);

    boolean existsById(int weaponId);
}
