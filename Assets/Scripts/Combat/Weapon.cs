﻿using System;
using System.Collections;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;

#endif

namespace Infection.Combat
{
    public class Weapon : MonoBehaviour
    {
        [SerializeField] private WeaponItem[] heldWeapons = new WeaponItem[2];
        [SerializeField] private float range = 100f;

        public WeaponState CurrentState => currentState;
        public WeaponItem CurrentWeapon
        {
            get => heldWeapons[currentWeaponIndex];
            private set => heldWeapons[currentWeaponIndex] = value;
        }

        public bool IsFullOfWeapons => !Array.Exists(heldWeapons, w => w == null);

        private CameraController m_CameraController = null;
        private int currentWeaponIndex = 0;
        private WeaponState currentState = WeaponState.Idle;
        private bool aimingDownSights = false;
        private float timeSinceFire = Mathf.Infinity;

        public enum WeaponState
        {
            Idle,
            Firing,
            Reloading,
            Switching
        }

        private void Start()
        {
            m_CameraController = GetComponent<CameraController>();
        }

        private void Update()
        {
            if (CurrentWeapon.WeaponDefinition)
            {
                // Automatic fire
                if (Input.GetButton("Fire"))
                {
                    StartCoroutine(FireWeapon());
                }

                // Reload weapon
                if (Input.GetButtonDown("Reload"))
                {
                    StartCoroutine(ReloadWeapon());
                }
            }
        }

        public void EquipWeapon(WeaponItem newWeapon)
        {
            // Player has no weapons
            // TODO: Find solution for edge case where player has other weapons but current weapon is null
            if (CurrentWeapon == null)
            {
                CurrentWeapon = newWeapon;
                return;
            }

            // Player has an empty slot in inventory
            int emptySlot = Array.FindIndex(heldWeapons, w => w == null);
            if (emptySlot > -1)
            {
                // Equip the new weapon and switch to it
                heldWeapons[emptySlot] = newWeapon;
                StartCoroutine(SwitchWeapon((currentWeaponIndex + 1) % heldWeapons.Length));
            }
            else
            {
                // No more space in inventory, replace current weapon with new one
                WeaponItem old = ReplaceWeapon(currentWeaponIndex, newWeapon);
                Debug.Log("Replaced " + old.WeaponDefinition.WeaponName + " with " + newWeapon.WeaponDefinition.WeaponName);
            }
        }

        /// <summary>
        /// Replaces a weapon from inventory at an index with a new weapon.
        /// </summary>
        /// <param name="index">Index of weapon inventory</param>
        /// <param name="newWeapon">New weapon to replace old</param>
        /// <returns>Old weapon that was replaced</returns>
        public WeaponItem ReplaceWeapon(int index, WeaponItem newWeapon)
        {
            WeaponItem oldWeapon = CurrentWeapon;
            CurrentWeapon = newWeapon;
            return oldWeapon;
        }

        /// <summary>
        /// Fire the currently equipped weapon.
        /// </summary>
        /// <returns>Firing state</returns>
        public IEnumerator FireWeapon()
        {
            // Cannot fire weapon when state is not idle
            if (currentState != WeaponState.Idle)
            {
                yield break;
            }

            // Out of ammo
            if (CurrentWeapon.Magazine <= 0)
            {
                if (CurrentWeapon.Reserves <= 0)
                {
                    // TODO: Display a message in the HUD to indicate that the player has no more ammo
                    Debug.Log("Out of ammo!");
                    // Switch to a different weapon if it exists and if it still has ammo left
                    int nextWeapon = Array.FindIndex(heldWeapons, w => w != null && w.Magazine + w.Reserves > 0);
                    if (nextWeapon > -1)
                    {
                        StartCoroutine(SwitchWeapon(nextWeapon));
                    }
                    yield break;
                }

                StartCoroutine(ReloadWeapon());
                yield break;
            }

            // Fire the weapon
            currentState = WeaponState.Firing;
            if (m_CameraController && Physics.Raycast(m_CameraController.currentCamera.transform.position, m_CameraController.currentCamera.transform.forward, out var hit, range))
            {
                Debug.Log(CurrentWeapon.WeaponDefinition.WeaponName + " hit target " + hit.transform.name);
            }

            // Subtract ammo and wait for next shot
            CurrentWeapon.ConsumeMagazine(1);
            yield return new WaitForSeconds(CurrentWeapon.WeaponDefinition.FireRate);
            currentState = WeaponState.Idle;
        }

        /// <summary>
        /// Reloads currently equipped weapon.
        /// </summary>
        /// <returns>Reload state</returns>
        public IEnumerator ReloadWeapon()
        {
            // Already reloading or not in idle state
            if (currentState == WeaponState.Reloading || currentState != WeaponState.Idle)
            {
                yield break;
            }

            // No more ammo
            if (CurrentWeapon.Reserves <= 0)
            {
                Debug.Log("No more ammo in reserves!");
                // TODO: Display a message in the HUD to indicate that the player has no more ammo
                yield break;
            }

            // Weapon already fully reloaded
            if (CurrentWeapon.Magazine >= CurrentWeapon.WeaponDefinition.ClipSize)
            {
                Debug.Log("Magazine fully loaded, no need to reload.");
                // TODO: Display a message in the HUD to indicate that the magazine is already filled up
                yield break;
            }

            // Reloading animation
            currentState = WeaponState.Reloading;
            // TODO: Play animation
            yield return new WaitForSeconds(CurrentWeapon.WeaponDefinition.ReloadTime);

            // Fill up magazine with ammo from reserves
            CurrentWeapon.ReloadMagazine();
            currentState = WeaponState.Idle;
        }

        /// <summary>
        /// Switch current weapon to another held weapon by index.
        /// </summary>
        /// <param name="index">Index of weapon to switch to</param>
        /// <returns>Switching state</returns>
        public IEnumerator SwitchWeapon(int index)
        {
            // Cannot switch weapon not in idle state
            if (currentState != WeaponState.Idle)
            {
                yield break;
            }

            // Begin switching weapons
            currentState = WeaponState.Switching;
            Debug.Log("Switching weapon");
            // TODO: Play putting away weapon animation
            yield return new WaitForSeconds(CurrentWeapon.WeaponDefinition.HolsterTime);

            currentWeaponIndex = index;
            // TODO: Play pulling out weapon animation
            yield return new WaitForSeconds(CurrentWeapon.WeaponDefinition.ReadyTime);
            Debug.Log("Weapon switch done");
            currentState = WeaponState.Idle;
        }
    }
}
