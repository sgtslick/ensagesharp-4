using System;
using System.Collections.Generic;
using System.Linq;
using Ensage;
using Ensage.Common.Extensions;
using SharpDX;

namespace LastHitMarker
{
    internal class Program
    {
        private static readonly Dictionary<Unit, string> CreepsDictionary = new Dictionary<Unit, string>();
        private static readonly Dictionary<Unit, Team> CreepsTeamDictionary = new Dictionary<Unit, Team>();
        private static Hero _me;
        private static Bool _screenSizeLoaded = false;
        private static float _screenX;
        private static void Main()
        {
            Game.OnUpdate += Game_OnUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
        }

        private static void Game_OnUpdate(EventArgs args)
        {
            if (!Game.IsInGame || Game.GameTime > 1800 || Game.IsPaused)
                return;

            var player = ObjectMgr.LocalPlayer;
            _me = player.Hero;

            if (player == null || player.Team == Team.Observer || _me == null || _me.MinimumDamage > 160)
                return;

            var quellingBlade = _me.FindItem(" item_quelling_blade ");
            var damage = (quellingBlade != null) ? (_me.MinimumDamage * 1.40 + _me.BonusDamage) : (_me.MinimumDamage + _me.BonusDamage);

            var creeps = ObjectMgr.GetEntities<Unit>().Where(creep => (creep.ClassID == ClassID.CDOTA_BaseNPC_Creep_Lane || creep.ClassID == ClassID.CDOTA_BaseNPC_Creep_Siege) && creep.IsSpawned).ToList();

            if (!creeps.Any())
                return;

            foreach (var creep in creeps) {
                if (creep.IsAlive && creep.IsVisible) {
                    string creepType;
                    if (creep.Health > 0 && creep.Health < damage*(1 - creep.DamageResist) + 1)
                    {
                        if (!CreepsDictionary.TryGetValue(creep, out creepType) || creepType != "passive") continue;
                        CreepsDictionary.Remove(creep);
                        CreepsTeamDictionary.Remove(creep);
                        creepType = "active";
                        CreepsDictionary.Add(creep, creepType);
                        CreepsTeamDictionary.Add(creep, creep.Team);
                    }
                    else if (creep.Health < damage + 88)
                    {
                        if (CreepsDictionary.TryGetValue(creep, out creepType)) continue;
                        creepType = "passive";
                        CreepsDictionary.Add(creep, creepType);
                    }
                }
                else {
                    CreepsDictionary.Remove(creep);
                    CreepsTeamDictionary.Remove(creep);
                }
            }
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            if (!Game.IsInGame || Game.GameTime > 1800)
                return;

            var creeps = ObjectMgr.GetEntities<Unit>().Where(creep => (creep.ClassID == ClassID.CDOTA_BaseNPC_Creep_Lane || creep.ClassID == ClassID.CDOTA_BaseNPC_Creep_Siege) && creep.IsSpawned).ToList();
            foreach (var creep in creeps) {
                Vector2 screenPos;
                var enemyPos = creep.Position + new Vector3(0, 0, creep.HealthBarOffset);
                if (!Drawing.WorldToScreen(enemyPos, out screenPos)) continue;

                var start = screenPos + new Vector2(-5, -30);

                string creepType;
                Team creepTeam;
                if (!CreepsDictionary.TryGetValue(creep, out creepType)) continue;
                if (!CreepsTeamDictionary.TryGetValue(creep, out creepTeam)) continue;

                if (!_screenSizeLoaded)
                {
                    _screenX = Drawing.Width / (float)1600 * (float)0.8;
                    _screenSizeLoaded = true;
                }
                
                switch (creepType) {
                    case "active": Drawing.DrawRect(start, creepTeam == _me.Team ? new Vector2(20 * _screenX, 20 * _screenX) : new Vector2(15*_screenX, 15*_screenX), creepTeam == _me.Team ? Drawing.GetTexture("materials/ensage_ui/other/active_deny.vmat") : Drawing.GetTexture("materials/ensage_ui/other/active_coin.vmat")); break;
                    case "passive": Drawing.DrawRect(start, creepTeam == _me.Team ? new Vector2(20 * _screenX, 20 * _screenX) : new Vector2(15 * _screenX, 15 * _screenX), creepTeam == _me.Team ? Drawing.GetTexture("materials/ensage_ui/other/passive_deny.vmat") : Drawing.GetTexture("materials/ensage_ui/other/passive_coin.vmat")); break;
                }
            }
        }
    }
}
