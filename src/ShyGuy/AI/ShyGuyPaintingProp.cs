using GameNetcodeStuff;
using Mono.Cecil;
using ShyGuy.AI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
using static UnityEngine.GraphicsBuffer;

namespace Scopophobia
{
    public class ShyGuyPaintingProp : PhysicsProp 
    {
        [Header("Painting Settings")]
        public List<PlayerControllerB> oldTarget = new List<PlayerControllerB>();//change this to a list so we can save more players than just one for each painting.
        public PlayerControllerB? targetPlayer;
        public int randomChance;
        private bool updatedScannode;

        private bool isTriggered;
        public bool hasSpawnedFromPickup;
        public bool hasSpawnedFromBeltBag;
        public bool hasSpawnedFromInteract;
        private bool isForceSpawn;
        private ScanNodeProperties scanNode;
        public AudioSource PaintingSound;

        [Header("Painting Audio")]
        public AudioClip[] PaintingCrySFX;
        public AudioClip[] fearSFX;

        public override int GetItemDataToSave()
        {
            return base.GetItemDataToSave();
        }
        public void Awake()
        {

        }
        public override void Start()
        {
            base.Start();
            try
            {
                scanNode = GetComponentInChildren<ScanNodeProperties>();
                if (Config.hidePaintingName) 
                { 
                    UpdateScannode(1);
                }
            }
            catch { ScopophobiaPlugin.Instance.LogInfoExtended("Failed to Init Shy Guy Painting"); }
        }

        public override void GrabItem()
        {
            base.GrabItem();
            ScopophobiaPlugin.logger.LogInfo($"Shy Guy Painting Grabbed. Am I Owner?: {IsOwner}");
            if (playerHeldBy != null)
            {
                if (!CanTriggerPainting()) return;
                if (!updatedScannode) UpdateScannode(2);
                isTriggered = true;
                targetPlayer = playerHeldBy;
                randomChance = UnityEngine.Random.Range(0, 100);
                var ShyGuy = UnityEngine.Object.FindObjectOfType<ShyGuyAI>();
                if (randomChance < Mathf.Clamp(Config.ChanceOfShyGuy, 0, 100) && !hasSpawnedFromPickup)
                {
                    if (ShyGuy != null && ShyGuy.hasBeenSpawned)
                    {
                        oldTarget.Add(playerHeldBy);//fix multiple spawning via players
                        if (ShyGuy.currentBehaviourStateIndex != 1 || ShyGuy.currentBehaviourStateIndex != 2)
                            ShyGuy.SwitchToBehaviourState(1);
                        StartCoroutine(InitializeAI(ShyGuy, playerHeldBy));
                        ScopophobiaPlugin.Instance.LogInfoExtended($"Triggering Already Spawned Shy Guy!");
                    }
                    else if (ShyGuy == null)
                    {
                        PlayAudioFX(fearSFX);
                        StartSpawnShyGuy();
                        hasSpawnedFromPickup = true;
                        oldTarget.Add(playerHeldBy);//fix multiple spawning via players
                        ScopophobiaPlugin.Instance.LogInfoExtended("Random chance met, spawning a shy guy from Pickup");
                    }
                }
                else
                {
                    PlayAudioFX(PaintingCrySFX);
                    ResetSpawnState();
                    ScopophobiaPlugin.Instance.LogInfoExtended("Survived Spawn Attempt");
                    if (IsOwner)
                    {
                        HUDManager.Instance.DisplayTip("There's an odd sound", "There's an odd sound emanating from the painting, better be careful!", false, false, "LC_ShyGuyPaintingTip1");
                    }
                }
            }
        }

        public void UpdateScannode(int which = 1)
        {
            switch (which)
            {
                case 1: scanNode.headerText = Config.hidePaintingName && !string.IsNullOrWhiteSpace(Config.nameToUseForPainting) ? Config.nameToUseForPainting : "Painting"; break;
                case 2: scanNode.headerText = "Odd Painting of SCP-096"; updatedScannode = true; break;
            }
        }
        public void TriggerFromBeltBag(PlayerControllerB player)
        {
            if (!hasSpawnedFromBeltBag && !isTriggered && !isHeldByEnemy && oldTarget.Contains(player) && StartOfRound.Instance.shipHasLanded && StartOfRound.Instance.timeSinceRoundStarted >= 2f && StartOfRound.Instance.currentLevel.spawnEnemiesAndScrap) return;
            isTriggered = true;
            targetPlayer = player;
            randomChance = UnityEngine.Random.Range(0, 100);
            var ShyGuy = UnityEngine.Object.FindObjectOfType<ShyGuyAI>();
            if (randomChance < Mathf.Clamp(Config.ChanceOfShyGuy, 0, 100) && !hasSpawnedFromBeltBag)
            {
                if (ShyGuy != null && ShyGuy.hasBeenSpawned)
                {
                    oldTarget.Add(player);//fix multiple spawning via players
                    if (ShyGuy.currentBehaviourStateIndex != 1)
                        ShyGuy.SwitchToBehaviourState(1);
                    StartCoroutine(InitializeAI(ShyGuy, player));
                    ScopophobiaPlugin.Instance.LogInfoExtended($"Triggering Already Spawned Shy Guy!");
                }
                else if (ShyGuy == null)
                {
                    PlayAudioFX(fearSFX);
                    StartSpawnShyGuy();
                    hasSpawnedFromBeltBag = true;
                    oldTarget.Add(player);//fix multiple spawning via players
                    ScopophobiaPlugin.Instance.LogInfoExtended("Random chance met, spawning a shy guy from Pickup");
                }
            }
            else
            {
                PlayAudioFX(PaintingCrySFX);
                ResetSpawnState();
            }
        }


        private bool CanTriggerPainting()
        {
            return isHeld && !hasSpawnedFromPickup && !isTriggered && !isHeldByEnemy && playerHeldBy != null && IsOwner && !oldTarget.Contains(playerHeldBy) && StartOfRound.Instance.shipHasLanded && StartOfRound.Instance.timeSinceRoundStarted >= 2f && StartOfRound.Instance.currentLevel.spawnEnemiesAndScrap;
        }
        public override void Update()
        {
            base.Update();
        }
       /* public override void Update()
        {
            base.Update();

            // Return early if not held or already completed the effect, or if player is old target, or not owner, ship landed, etc
            if (!CanTriggerPainting()) return;
            if (!updatedScannode)
            {
                UpdateScannode(2);//update scannode back to odd painting of SCP
            }
            isTriggered = true;
            targetPlayer = GameNetworkManager.Instance.localPlayerController;
            ScopophobiaPlugin.Instance.LogInfoExtended($"Shy Guy Painting triggered by {targetPlayer.playerUsername}");

            randomChance = UnityEngine.Random.Range(0, 100);
            var ShyGuy = UnityEngine.Object.FindObjectOfType<ShyGuyAI>();
            if (randomChance < Mathf.Clamp(Config.ChanceOfShyGuy, 0, 100) && !hasSpawnedFromPickup)
            {
                if (ShyGuy != null && ShyGuy.hasBeenSpawned)
                {
                      oldTarget.Add(playerHeldBy);//fix multiple spawning via players
                    if (ShyGuy.currentBehaviourStateIndex != 1)
                        ShyGuy.SwitchToBehaviourState(1);
                    StartCoroutine(InitializeAI(ShyGuy, playerHeldBy));
                    ScopophobiaPlugin.Instance.LogInfoExtended($"Triggering Already Spawned Shy Guy!");
                }
                else if(ShyGuy == null)
                {
                    PlayAudioFX(fearSFX);
                    StartSpawnShyGuy();
                    hasSpawnedFromPickup = true;
                    oldTarget.Add(playerHeldBy);//fix multiple spawning via players
                    ScopophobiaPlugin.Instance.LogInfoExtended("Random chance met, spawning a shy guy from Pickup");
                }
            }
            else
            {
                PlayAudioFX(PaintingCrySFX);
                ResetSpawnState();
                ScopophobiaPlugin.Instance.LogInfoExtended("Survived Spawn Attempt");
                if (IsOwner)
                {
                    HUDManager.Instance.DisplayTip("There's an odd sound", "There's an odd sound emanating from the painting, better be careful!", false, false, "LC_ShyGuyPaintingTip1");
                }
            }
        }*/
       
        public void PlayAudioFX(AudioClip[] clip)
        {
            if (PaintingSound == null) return;
            if (clip == null) return;
            int num = UnityEngine.Random.Range(0, clip.Length);
            PaintingSound.clip = clip[num];
            PaintingSound.volume = 0.3f;
            PaintingSound.Play();
        }

        public void StartSpawnShyGuy() 
        {
            int targetId = (int)playerHeldBy.actualClientId;
            SpawnEnemyServerRpc(targetId);
        }
        [ServerRpc(RequireOwnership = false)]
        public void SpawnEnemyServerRpc(int targetId)
        {
            if (!IsServer) { ScopophobiaPlugin.Instance.LogErrorExtended($"[ERROR] Client {NetworkUtils.GetLocalClientId()} called SpawnShyGuyOnServer, this is server only"); }
            PlayerControllerB target = StartOfRound.Instance.allPlayerScripts[targetId];
            if (targetId < 0 || targetId >= StartOfRound.Instance.allPlayerScripts.Length)
            {
                ScopophobiaPlugin.Instance.LogErrorExtended($"Invalid target id {targetId}");
                return;
            }
            Vector3 spawnPos = RoundManager.Instance.GetRandomNavMeshPositionInRadius(target.transform.position, 8f, default);
            ScopophobiaPlugin.Instance.LogInfoExtended($"[SpawnEnemyOnServer] Triggered by client {targetId} ({StartOfRound.Instance.allPlayerScripts[targetId].playerUsername}), Host running Check: {IsServer}");
            SpawnableEnemyWithRarity? enemy = RoundManager.Instance.currentLevel.Enemies.Find(x => x.enemyType.enemyName.Equals("Shy Guy", StringComparison.OrdinalIgnoreCase));
            if (enemy == null)//if enemy not found, shy guy not included in level enemies?
            {
                ScopophobiaPlugin.Instance.LogInfoExtended("Shy Guy Enemy Not found in level, trying local Asset");
                try
                {
                    if (NetworkUtils.IsNetworkPrefab(ScopophobiaPlugin.shyGuy.enemyPrefab))
                    {
                        enemy = ScopophobiaPlugin.shyEnemy;
                    }
                }
                catch (Exception ex)
                {
                    ScopophobiaPlugin.Instance.LogErrorExtended($"FATAL ERROR! Shy Guy is not a registered NetworkPrefab: {ex.ToString()}");
                    return;
                }
            }
            GameObject obj = RoundManager.Instance.SpawnEnemyGameObject(spawnPos,0f,1,enemy?.enemyType);
            //obj.GetComponent<NetworkObject>().Spawn(destroyWithScene: true);
            //RoundManager.Instance.SpawnEnemyGameObject(spawnPos, 0f, 1, enemy.enemyType);
            if (obj == null) { ScopophobiaPlugin.Instance.LogErrorExtended("Failed to spawn Shy Guy"); return; }
            ShyGuyAI ai = obj.GetComponent<ShyGuyAI>();
            NetworkObject netObj = obj.GetComponent<NetworkObject>();
            if (ai == null) { ScopophobiaPlugin.Instance.LogErrorExtended("SHY Guy AI is Null"); return; }
            ai.ChangeOwnershipOfEnemy(target.actualClientId);
            //SpawnEnemyClientRpc(obj, (int)target.actualClientId);
            StartCoroutine(InitializeAI(ai, target));
        }
        public void ResetSpawnState() {
            isTriggered = false;
            if (targetPlayer != null && !oldTarget.Contains(targetPlayer))
            { oldTarget.Add(targetPlayer); }
            targetPlayer = null;
            randomChance = 0;
        }
        [ClientRpc]
        public void SpawnEnemyClientRpc(NetworkObjectReference netObj, int targetId)
        {
            if (netObj.TryGet(out var shyGuy))
            {
                var target = StartOfRound.Instance.allPlayerScripts[targetId];
                if (target == null) return;
                var shyGuyAI = shyGuy.GetComponent<ShyGuyAI>();
                if (shyGuyAI == null) return;
                StartCoroutine(InitializeAI(shyGuyAI, target));
            }
        }
        private IEnumerator InitializeAI(ShyGuyAI ai, PlayerControllerB target)
        {
            if (ai != null && ai.isActiveAndEnabled)
            {
                if(ai.currentBehaviourStateIndex != 1) ai.SwitchToBehaviourState(1);
                yield return new WaitForSeconds(Config.triggerTime);//delay by trigger
                ai.AddTargetToList((int)target.actualClientId, false, "Painting");
                ai.targetPlayer = target;
                ai.ChangeOwnershipOfEnemy(target.actualClientId);
            }
            ResetSpawnState();
        }
    }
}
