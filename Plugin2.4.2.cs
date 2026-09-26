
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using Comfort.Common;
using EFT;
using EFT.HealthSystem;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace SPTBotCounter
{
    public enum TrackedBotType { Pmc, Scav, Rogue, Raider, Boss, Ignore }

    /// <summary>界面语言（F12 里可切换）。</summary>
    public enum ModLanguage { Chinese = 0, English = 1 }

    public struct BossRenderInfo
    {
        public string Text;      // 保留：诊断面板/回退用
        public Color Color;
        public int RawRole;
        public float Distance;
        public string RoleStr;   // 角色枚举名，用于实时重算名字
    }

    internal class ConfigurationManagerAttributes
    {
        public int? Order;
    }

    /// <summary>
    /// 全部文案集中在这里：index 0 = 中文，index 1 = English。
    /// </summary>
    internal static class Loc
    {
        public static ModLanguage Lang = ModLanguage.Chinese;

        private static readonly Dictionary<string, string[]> T = new Dictionary<string, string[]>
        {
            { "section.visibility",  new[]{ "1. 显示开关",       "1. Visibility" } },
            { "section.display",     new[]{ "2. 显示设置",       "2. Display" } },
            { "section.colors",      new[]{ "3. 颜色",           "3. Colors" } },
            { "section.diagnostics", new[]{ "4. 诊断",           "4. Diagnostics" } },
            { "section.teammates",   new[]{ "5. 队友识别",       "5. Teammates" } },

            { "cfg.enable",          new[]{ "开关模组",              "Enable Mod" } },
            { "cfg.showPmc",         new[]{ "显示 PMC 数量",         "Show PMCs" } },
            { "cfg.showScav",        new[]{ "显示 Scav 数量",        "Show Scavs" } },
            { "cfg.showRogue",       new[]{ "显示 Rogue 数量",       "Show Rogues" } },
            { "cfg.showRaider",      new[]{ "显示 Raider 数量",      "Show Raiders" } },
            { "cfg.showBoss",        new[]{ "显示 Boss",             "Show Bosses" } },
            { "cfg.hideZero",        new[]{ "隐藏数量为 0 的分类",    "Hide Zero Counts" } },
            { "cfg.showNearest",     new[]{ "显示最近距离",          "Show Distance" } },
            { "cfg.bossColors",      new[]{ "Boss 血量变色",         "Boss Health Colors" } },
            { "cfg.refresh",         new[]{ "刷新间隔（秒）",         "Refresh Rate (Seconds)" } },
            { "cfg.fontSize",        new[]{ "字号",                  "Font Size" } },
            { "cfg.offsetRight",     new[]{ "距屏幕右侧距离",         "Offset Right" } },
            { "cfg.offsetTop",       new[]{ "距屏幕顶部距离",         "Offset Top" } },
            { "cfg.opacity",         new[]{ "不透明度（%）",          "Opacity (%)" } },
            { "cfg.globalColor",     new[]{ "全局文字颜色",          "Global Text Color" } },
            { "cfg.language",        new[]{ "界面语言",              "Language" } },
            { "cfg.debugOverlay",    new[]{ "调试面板",              "Debug Overlay" } },
            { "cfg.debugLog",        new[]{ "调试日志",              "Debug Logging" } },
            { "cfg.excludeTeammates",new[]{ "排除队友（不计入任何数量）", "Exclude Teammates" } },
            { "cfg.tmPitTeam",       new[]{ "识别 pitFireTeam 队友",  "Detect pitFireTeam Teammates" } },
            { "cfg.tmAllies",        new[]{ "识别与玩家同组的 bot",    "Detect Player-Allied Bots" } },
            { "cfg.showTeammates",   new[]{ "单独显示队友数量",       "Show Teammate Count" } },

            { "desc.enable",         new[]{ "一键开关整个模组。", "Turn the entire mod on or off." } },
            { "desc.showPmc",        new[]{ "显示地图上 PMC 的数量。", "Show the amount of PMCs on the map." } },
            { "desc.showScav",       new[]{ "显示地图上 Scav 的数量。", "Show the amount of Scavs on the map." } },
            { "desc.showRogue",      new[]{ "显示地图上 Rogue 的数量。", "Show the amount of Rogues on the map." } },
            { "desc.showRaider",     new[]{ "显示地图上 Raider 的数量。", "Show the amount of Raiders on the map." } },
            { "desc.showBoss",       new[]{ "显示当前存活的 Boss 及其名称。", "Show active bosses and their names." } },
            { "desc.hideZero",       new[]{ "某一分类存活数为 0 时，整行不显示。", "Hides a category completely if there are 0 bots of that type alive." } },
            { "desc.showNearest",    new[]{ "显示每一类中距离最近的 bot 有多远。", "Shows the distance to the closest bot of each category." } },
            { "desc.bossColors",     new[]{ "根据 Boss 剩余血量改变名字颜色。", "Changes the color of boss names based on their remaining health." } },
            { "desc.refresh",        new[]{ "距离与计数的刷新频率，数值越大越省性能。", "How often the distances and counters update. Higher = better performance." } },
            { "desc.fontSize",       new[]{ "调整文字大小。", "Adjust the text size." } },
            { "desc.offsetRight",    new[]{ "把界面从屏幕右边缘往左移。", "Move the UI away from the right edge of the screen." } },
            { "desc.offsetTop",      new[]{ "把界面从屏幕顶部往下移。", "Move the UI down from the top edge of the screen." } },
            { "desc.opacity",        new[]{ "文字透明度（10% 几乎看不见，100% 完全不透明）。", "Set the transparency of the text (10% = nearly invisible, 100% = solid)." } },
            { "desc.globalColor",    new[]{ "所有普通文字条目的默认颜色。", "The default color for all normal text entries." } },
            { "desc.language",       new[]{ "界面语言。局内文字立即生效；F12 菜单的分组名和条目名需要重启游戏后生效。", "Interface language. In-raid text applies immediately; F12 section and entry names apply after a game restart." } },
            { "desc.debugOverlay",   new[]{ "在屏幕左上角显示诊断面板（进图判定 / bot 数量 / 识别到的角色）。一切正常后建议关闭。", "Shows a diagnostic panel on the LEFT side of the screen. Turn this OFF once everything works." } },
            { "desc.debugLog",       new[]{ "每秒往 BepInEx\\LogOutput.log 写一行诊断信息。", "Writes diagnostic lines to BepInEx\\LogOutput.log once per second while in raid." } },
            { "desc.excludeTeammates",new[]{ "开启后，被识别为队友的 bot 完全不计入 PMC / Scav / Rogue / Raider / Boss 任何一类，也不再参与最近距离计算。", "When on, bots identified as teammates are excluded from every counter (PMC / Scav / Rogue / Raider / Boss) and from the nearest-distance calculation." } },
            { "desc.tmPitTeam",      new[]{ "读取 pitFireTeam 的队友登记表（pitTeam.Modules.BossPlayers）来识别它生成的队友。没装 pitFireTeam 时这条规则自动跳过。", "Identifies teammates via pitFireTeam's follower registry (pitTeam.Modules.BossPlayers). Automatically skipped when pitFireTeam is not installed." } },
            { "desc.tmAllies",       new[]{ "通用规则：凡是它所在的 Bot 小组把玩家列为友军（Allies）的 bot 都算队友。适用于没有专门登记表的队友类 mod。", "Generic rule: any bot whose BotsGroup lists the player as an ally counts as a teammate. Useful for teammate mods without a dedicated registry." } },
            { "desc.showTeammates",  new[]{ "在界面上单独显示被排除的队友数量，方便确认识别是否生效。", "Shows the number of excluded teammates on the overlay so you can confirm detection works." } },

            { "ui.pmc",              new[]{ "PMC",   "PMC" } },
            { "ui.scav",             new[]{ "Scav",  "Scav" } },
            { "ui.rogue",            new[]{ "Rogue", "Rogue" } },
            { "ui.raider",           new[]{ "Raider","Raider" } },
            { "ui.boss",             new[]{ "Boss",  "Boss" } },
            { "ui.bosses",           new[]{ "Bosses","Bosses" } },
            { "ui.teammate",         new[]{ "队友（已排除）", "Teammates (excluded)" } },
        };

        public static string Get(string key)
        {
            string[] v;
            if (T.TryGetValue(key, out v)) return v[(int)Lang];
            return key;
        }

        // 中文译名来源：takefu.cn Boss 图鉴（https://takefu.cn/boss/index.html）
        // 查不到官方/通用译名的角色保留英文原名。
        private static readonly Dictionary<string, string[]> BossNames = new Dictionary<string, string[]>
        {
            // ---- 首领 ----
            { "bossbully",               new[]{ "雷沙拉",         "Reshala" } },
            { "bosskojaniy",             new[]{ "施图尔曼",        "Shturman" } },
            { "bossgluhar",              new[]{ "格鲁哈尔",        "Glukhar" } },
            { "bosssanitar",             new[]{ "赛尼塔",         "Sanitar" } },
            { "bosskilla",               new[]{ "基拉",           "Killa" } },
            { "bosskillaagro",           new[]{ "基拉",           "Killa" } },
            { "bosstagilla",             new[]{ "塔基拉",         "Tagilla" } },
            { "bosstagillaagro",         new[]{ "塔基拉",         "Tagilla" } },
            { "bosszryachiy",            new[]{ "兹里亚奇",        "Zryachiy" } },
            { "bossknight",              new[]{ "骑士",           "Knight" } },
            { "bossboar",                new[]{ "卡班",           "Kaban" } },
            { "bossboarsniper",          new[]{ "卡班狙击护卫",     "Kaban Guard (Sniper)" } },
            { "bosskolontay",            new[]{ "科隆泰",         "Kollontay" } },
            { "bosspartisan",            new[]{ "游击队",         "Partisan" } },

            // ---- 护卫 / 随从 ----
            { "followerbully",           new[]{ "雷沙拉护卫",      "Reshala Guard" } },
            { "followerkojaniy",         new[]{ "施图尔曼护卫",     "Shturman Guard" } },
            { "followergluharassault",   new[]{ "格鲁哈尔护卫（突击）", "Glukhar Guard (Assault)" } },
            { "followergluharsecurity",  new[]{ "格鲁哈尔护卫（安保）", "Glukhar Guard (Security)" } },
            { "followergluharscout",     new[]{ "格鲁哈尔护卫（侦察）", "Glukhar Guard (Scout)" } },
            { "followergluharsnipe",     new[]{ "格鲁哈尔护卫",      "Glukhar Guard" } },
            { "followersanitar",         new[]{ "赛尼塔护卫",      "Sanitar Guard" } },
            { "followerboar",            new[]{ "卡班护卫",        "Kaban Guard" } },
            { "followerboarclose1",      new[]{ "卡班护卫",        "Kaban Guard" } },
            { "followerboarclose2",      new[]{ "卡班护卫",        "Kaban Guard" } },
            { "followerkolontayassault", new[]{ "科隆泰护卫（突击）", "Kollontay Guard (Assault)" } },
            { "followerkolontaysecurity",new[]{ "科隆泰护卫（安保）", "Kollontay Guard (Security)" } },
            { "followerzryachiy",        new[]{ "兹里亚奇护卫",     "Zryachiy Guard" } },
            { "followerbigpipe",         new[]{ "大管",           "Big Pipe" } },
            { "followerbirdeye",         new[]{ "鸟眼",           "Birdeye" } },

            // ---- 活动/特殊（译名查不到 → 保留英文）----
            { "shooterbtr",              new[]{ "BTR Gunner",    "BTR Gunner" } },
            { "peacemaker",              new[]{ "Peacemaker",    "Peacemaker" } },
            { "skier",                   new[]{ "Skier",        "Skier" } },
        };

        // ==================== 第三方 mod 的 Boss 注册表 ====================
        // 其他 mod 通过 BotCounterApi 注册后，这里的条目优先于内置译名。
        private static readonly Dictionary<string, string> CustomNames =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>注册/覆盖一个角色的显示名。返回 true 表示新增，false 表示覆盖了已有条目。</summary>
        internal static bool RegisterCustomName(string role, string displayName)
        {
            if (string.IsNullOrEmpty(role) || string.IsNullOrEmpty(displayName)) return false;
            string key = role.Trim().ToLowerInvariant();
            bool isNew = !CustomNames.ContainsKey(key);
            CustomNames[key] = displayName.Trim();
            return isNew;
        }

        internal static bool UnregisterCustomName(string role)
        {
            if (string.IsNullOrEmpty(role)) return false;
            return CustomNames.Remove(role.Trim().ToLowerInvariant());
        }

        internal static void ClearCustomNames() { CustomNames.Clear(); }

        internal static int CustomNameCount { get { return CustomNames.Count; } }

        internal static bool IsCustomName(string roleLower)
        {
            return !string.IsNullOrEmpty(roleLower) && CustomNames.ContainsKey(roleLower);
        }

        public static string Boss(string roleLower)
        {
            if (string.IsNullOrEmpty(roleLower)) return null;

            // 1) 第三方 mod 注册的名字最优先（按原名显示）
            string custom;
            if (CustomNames.TryGetValue(roleLower, out custom)) return custom;

            // 2) 内置的特殊规则
            if (roleLower.StartsWith("follower") && roleLower.Contains("tagilla"))
                return Lang == ModLanguage.Chinese ? "塔基拉护卫" : "Tagilla Guard";

            // 3) 内置译名表
            string[] v;
            if (BossNames.TryGetValue(roleLower, out v)) return v[(int)Lang];
            return null;
        }

        public static string Category(string key, int count) { return Get(key) + "：" + count; }

        public static string CategoryWithDistance(string key, float meters, int count)
        {
            // 专有名词保持英文，只有单位和排版跟随语言
            return Lang == ModLanguage.Chinese
                ? Get(key) + "：" + string.Format("[{0:F0} 米] ", meters) + count
                : Get(key) + ": " + string.Format("[{0:F0}m] ", meters) + count;
        }

        public static string BossTitle(int alive)
        {
            return Get(alive > 1 ? "ui.bosses" : "ui.boss") + "：" + alive;
        }

        public static string BossLabel(string name, float distance, bool withDistance)
        {
            if (!withDistance) return name;
            return string.Format(Lang == ModLanguage.Chinese ? "[{0:F0} 米] {1}" : "[{0:F0}m] {1}", distance, name);
        }
    }

    /// <summary>
    /// BotCounter 对外接口 —— 供其他 mod 注册自己的 Boss。
    ///
    /// 用法（推荐，零依赖，不需要引用 BotCounter.dll）：
    /// <code>
    /// var t = Type.GetType("SPTBotCounter.BotCounterApi, BotCounter");
    /// t?.GetMethod("RegisterBoss")?.Invoke(null, new object[] { "myCustomBoss", "我的首领" });
    /// </code>
    ///
    /// 也可以直接引用本 DLL 后调用：
    /// <code>
    /// SPTBotCounter.BotCounterApi.RegisterBoss("myCustomBoss", "我的首领");
    /// </code>
    ///
    /// 注册之后：
    ///   1. 该角色会被计入 "Boss" 分类（即使 SPT 自己不认识它）；
    ///   2. 显示名直接用你给的名字，不会被内置译名覆盖；
    ///   3. 名字栏位固定显示该名字，Boss 血量变色照常生效。
    ///
    /// 角色名不限类型：WildSpawnType 枚举名，或你 mod 自己的自定义角色字符串都可以。
    /// </summary>
    /// <summary>
    /// Teammate marker table: records "this bot is a teammate" by ProfileId.
    /// Marked bots are excluded from every BotCounter count
    /// (PMC / Scav / Rogue / Raider / Boss and the nearest-distance calculation).
    ///
    /// This is the single exit point for BotCounter's own detection
    /// (pitFireTeam registry, player-allied BotsGroup) and it is public too:
    /// any mod can call BotCounterApi.MarkTeammate after spawning its own teammate bots.
    /// </summary>
    internal static class TeammateRegistry
    {
        private static readonly HashSet<string> Ids =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public static bool Mark(string profileId)
        {
            if (string.IsNullOrEmpty(profileId)) return false;
            return Ids.Add(profileId.Trim());
        }

        public static bool Unmark(string profileId)
        {
            if (string.IsNullOrEmpty(profileId)) return false;
            return Ids.Remove(profileId.Trim());
        }

        public static void Clear() { Ids.Clear(); }

        public static int Count { get { return Ids.Count; } }

        public static bool Contains(string profileId)
        {
            return !string.IsNullOrEmpty(profileId) && Ids.Contains(profileId.Trim());
        }
    }
    public static class BotCounterApi
    {
        /// <summary>
        /// 注册一个 Boss（或任意角色）的显示名。
        /// </summary>
        /// <param name="role">角色名，例如 "myCustomBoss"（大小写不敏感）</param>
        /// <param name="displayName">游戏里显示的名字，直接用你 mod 里的名字即可</param>
        /// <returns>true = 新增；false = 覆盖了已有条目（或参数为空）</returns>
        public static bool RegisterBoss(string role, string displayName)
        {
            bool isNew = Loc.RegisterCustomName(role, displayName);
            QueueRegistryChange();
            return isNew;
        }

        /// <summary>注册一个 Boss，角色名直接取枚举。</summary>
        public static bool RegisterBoss(Enum role, string displayName)
        {
            return RegisterBoss(role == null ? null : role.ToString(), displayName);
        }

        /// <summary>
        /// 标记"注册表变了"。只排队，不直接触碰插件实例 ——
        /// 这样即使 BotCounter 插件尚未初始化、或调用方是另一个 mod，也不会抛异常。
        /// 插件会在下一帧自己发现并重新分类。
        /// </summary>
        private static void QueueRegistryChange()
        {
            _pendingRegistryChange = true;
        }

        internal static bool ConsumeRegistryChange()
        {
            if (!_pendingRegistryChange) return false;
            _pendingRegistryChange = false;
            return true;
        }

        private static volatile bool _pendingRegistryChange = false;

        /// <summary>取消注册（回退到内置译名 / 昵称）。</summary>
        public static bool UnregisterBoss(string role)
        {
            bool removed = Loc.UnregisterCustomName(role);
            if (removed) QueueRegistryChange();
            return removed;
        }

        /// <summary>取消一个枚举角色的注册。</summary>
        public static bool UnregisterBoss(Enum role)
        {
            return UnregisterBoss(role == null ? null : role.ToString());
        }

        /// <summary>清空所有第三方注册项。</summary>
        public static void ClearRegisteredBosses()
        {
            Loc.ClearCustomNames();
            QueueRegistryChange();
        }

        /// <summary>当前已注册的第三方 Boss 数量（可用于自检）。</summary>
        public static int RegisteredBossCount { get { return Loc.CustomNameCount; } }

        /// <summary>查询某个角色当前会显示成什么名字（null = 未注册也未内置）。</summary>
        public static string GetBossDisplayName(string role)
        {
            return string.IsNullOrEmpty(role) ? null : Loc.Boss(role.Trim().ToLowerInvariant());
        }

        // ==================== teammate marker API ====================
        // For other mods: mark the ProfileId of your own teammate bots and
        // BotCounter will stop counting them as PMC / Scav / Boss.

        /// <summary>
        /// Mark a bot as a teammate by ProfileId. true = newly added, false = already marked or empty.
        /// ProfileId is botOwner.ProfileId (same as botOwner.Profile.Id) -- the very same key
        /// pitFireTeam uses internally.
        /// </summary>
        public static bool MarkTeammate(string profileId)
        {
            return TeammateRegistry.Mark(profileId);
        }

        /// <summary>Remove a teammate mark. true = it was actually removed.</summary>
        public static bool UnmarkTeammate(string profileId)
        {
            return TeammateRegistry.Unmark(profileId);
        }

        /// <summary>Clear all teammate marks.</summary>
        public static void ClearTeammates()
        {
            TeammateRegistry.Clear();
        }

        /// <summary>How many bots are currently marked as teammates.</summary>
        public static int MarkedTeammateCount { get { return TeammateRegistry.Count; } }

        /// <summary>Is this ProfileId marked as a teammate?</summary>
        public static bool IsMarkedTeammate(string profileId)
        {
            return TeammateRegistry.Contains(profileId);
        }
    }

    [BepInPlugin("com.spt.botcounter", "BotCounter", "2.4.2")]
    public class BotCounterPlugin : BaseUnityPlugin
    {
        // ---- 旧配置的英文键/分组名 → 新键/新分组名（用于无损迁移）----
        private static readonly Dictionary<string, string> KeyMap = new Dictionary<string, string>
        {
            { "Enable Mod",              "cfg.enable" },
            { "Show PMCs",               "cfg.showPmc" },
            { "Show Scavs",              "cfg.showScav" },
            { "Show Rogues",             "cfg.showRogue" },
            { "Show Raiders",            "cfg.showRaider" },
            { "Show Bosses",             "cfg.showBoss" },
            { "Hide Zero Counts",        "cfg.hideZero" },
            { "Show Distance",           "cfg.showNearest" },
            { "Boss Health Colors",      "cfg.bossColors" },
            { "Refresh Rate (Seconds)",  "cfg.refresh" },
            { "Font Size",               "cfg.fontSize" },
            { "Offset Right",            "cfg.offsetRight" },
            { "Offset Top",              "cfg.offsetTop" },
            { "Opacity (%)",             "cfg.opacity" },
            { "Global Text Color",       "cfg.globalColor" },
            { "Language",                "cfg.language" },
            { "Debug Overlay",           "cfg.debugOverlay" },
            { "Debug Logging",           "cfg.debugLog" },
        };

        private static readonly Dictionary<string, string> SectionMap = new Dictionary<string, string>
        {
            { "1. Visibility",    "section.visibility" },
            { "2. Display",       "section.display" },
            { "3. Colors",        "section.colors" },
            { "4. Diagnostics",   "section.diagnostics" },
        };

        private GUIStyle _guiStyle;
        private GUIStyle _debugStyle;
        private GUIStyle _errStyle;
        private Font _cjkFont;
        private ManualLogSource _log;

        private ConfigEntry<bool> _enableMod;
        private ConfigEntry<bool> _showPmc, _showScav, _showBoss, _showRogue, _showRaider, _showNearest, _hideZeroCounts, _bossHealthColors;
        private ConfigEntry<int> _fontSize, _offsetRight, _offsetTop, _opacity, _refreshRate;
        private ConfigEntry<Color> _globalColor;
        private ConfigEntry<ModLanguage> _language;
        private ConfigEntry<bool> _debugOverlay, _debugLog;
        private ConfigEntry<bool> _excludeTeammates, _tmPitTeam, _tmAllies, _showTeammates;

        private bool _inRaid = false;
        private float _nextUpdate = 0f;
        private ModLanguage _appliedLang;

        private int _cPmc, _cScav, _cRogue, _cRaider;
        private float _distPmc = -1f, _distScav = -1f, _distRogue = -1f, _distRaider = -1f;

        private readonly Dictionary<int, TrackedBotType> _allSeenBots = new Dictionary<int, TrackedBotType>();
        private readonly HashSet<int> _deadBotIds = new HashSet<int>();

        // ---- teammate detection ----
        private readonly HashSet<int> _teammateIds = new HashSet<int>();
        private int _cTeammates;                       // teammates excluded in this scan
        private int _tmHitPit, _tmHitAlly, _tmHitApi;  // which rule matched
        private string _tTeammate = "";
        private string _lastTeammate = "-";
        private string _teammateProbe = "未探测";

        // pitFireTeam probe results (reflection only, no compile-time dependency)
        private static PropertyInfo _pitInstanceProp;
        private static MethodInfo _pitIsFollowerProfileId;
        private static MethodInfo _pitIsBossGroup;
        private static float _nextPitProbe;

        private string _tPmc = "", _tScav = "", _tRogue = "", _tRaider = "", _tBossTitle = "";
        private readonly List<BossRenderInfo> _tBosses = new List<BossRenderInfo>();

        private string _lastError = "none";
        private string _lastBossResolve = "-";
        private string _lastClassify = "-";
        private int _rawBotCount = -1;
        private int _aliveCount = -1;
        private int _guiCalls = 0;
        private float _scanMs = -1f;
        private string _worldInfo = "-";
        private string _gameWorldType = "-";
        private readonly Dictionary<string, int> _roleTally = new Dictionary<string, int>();

        // ==================== 配置迁移（无损）====================

        /// <summary>
        /// 把旧版的英文分组名/键名就地改写成当前语言的名称，注释和值全部原样保留。
        /// 这样用户既拿到全中文菜单，又不会丢任何设置。
        /// </summary>
        private void MigrateConfigFile(ModLanguage lang)
        {
            try
            {
                // 注意：Assembly-CSharp 里也定义了全局的 Paths（路点），必须完全限定名
                string path = Path.Combine(BepInEx.Paths.ConfigPath, "com.spt.botcounter.cfg");
                if (!File.Exists(path)) return;

                string[] lines = File.ReadAllLines(path);
                var output = new List<string>(lines.Length);
                bool changed = false;
                string sectionId = null;

                foreach (string raw in lines)
                {
                    string trimmed = raw.Trim();

                    if (trimmed.StartsWith("[") && trimmed.EndsWith("]"))
                    {
                        string name = trimmed.Substring(1, trimmed.Length - 2).Trim();
                        string mapped;
                        if (SectionMap.TryGetValue(name, out mapped))
                        {
                            sectionId = mapped;
                            string newName = Loc.Get(mapped);
                            if (newName != name) { output.Add("[" + newName + "]"); changed = true; continue; }
                        }
                        else sectionId = null;

                        output.Add(raw);
                        continue;
                    }

                    if (sectionId != null && trimmed.Length > 0 && !trimmed.StartsWith("#"))
                    {
                        int eq = trimmed.IndexOf('=');
                        if (eq > 0)
                        {
                            string key = trimmed.Substring(0, eq).Trim();
                            string mapped;
                            if (KeyMap.TryGetValue(key, out mapped))
                            {
                                string newKey = Loc.Get(mapped);
                                if (newKey != key)
                                {
                                    output.Add(newKey + " = " + trimmed.Substring(eq + 1).Trim());
                                    changed = true;
                                    continue;
                                }
                            }
                        }
                    }

                    output.Add(raw);
                }

                if (changed)
                {
                    File.WriteAllLines(path, output.ToArray());
                    _log.LogInfo("[BotCounter] 配置文件已迁移为中文命名 / config keys renamed to Chinese.");
                }
            }
            catch (Exception ex)
            {
                _log.LogWarning("[BotCounter] 配置迁移失败（不影响使用）: " + ex.Message);
            }
        }

        // ==================== 生命周期 ====================

        private void Awake()
        {
            _log = Logger;

            // 先决定语言：从旧配置里读 Language，读不到就用默认（中文）
            ModLanguage lang = ModLanguage.Chinese;
            try
            {
                var langEntry = Config.Bind(Loc.Get("section.display"), Loc.Get("cfg.language"),
                    ModLanguage.Chinese, Loc.Get("desc.language"));
                lang = langEntry.Value;
            }
            catch { }

            Loc.Lang = lang;

            // 再按语言迁移旧键名，然后正式绑定
            MigrateConfigFile(lang);

            _enableMod = Bind("section.visibility", "cfg.enable", true, "desc.enable");
            _showPmc = Bind("section.visibility", "cfg.showPmc", true, "desc.showPmc");
            _showScav = Bind("section.visibility", "cfg.showScav", true, "desc.showScav");
            _showRogue = Bind("section.visibility", "cfg.showRogue", true, "desc.showRogue");
            _showRaider = Bind("section.visibility", "cfg.showRaider", true, "desc.showRaider");
            _showBoss = Bind("section.visibility", "cfg.showBoss", true, "desc.showBoss");
            _hideZeroCounts = Bind("section.visibility", "cfg.hideZero", false, "desc.hideZero");
            _showNearest = Bind("section.visibility", "cfg.showNearest", false, "desc.showNearest");
            _bossHealthColors = Bind("section.visibility", "cfg.bossColors", true, "desc.bossColors");

            _refreshRate = Bind("section.display", "cfg.refresh", 5, "desc.refresh", 1, 5);
            _fontSize = Bind("section.display", "cfg.fontSize", 16, "desc.fontSize", 8, 50);
            _offsetRight = Bind("section.display", "cfg.offsetRight", 10, "desc.offsetRight", 0, 1000);
            _offsetTop = Bind("section.display", "cfg.offsetTop", 70, "desc.offsetTop", 0, 1000);
            _opacity = Bind("section.display", "cfg.opacity", 100, "desc.opacity", 10, 100);

            _globalColor = Bind("section.colors", "cfg.globalColor", new Color(0f, 0.8f, 0f, 1f), "desc.globalColor");

            _language = Config.Bind(Loc.Get("section.display"), Loc.Get("cfg.language"),
                ModLanguage.Chinese, Loc.Get("desc.language"));

            _debugOverlay = Bind("section.diagnostics", "cfg.debugOverlay", false, "desc.debugOverlay");
            _debugLog = Bind("section.diagnostics", "cfg.debugLog", false, "desc.debugLog");

            _excludeTeammates = Bind("section.teammates", "cfg.excludeTeammates", true, "desc.excludeTeammates");
            _tmPitTeam = Bind("section.teammates", "cfg.tmPitTeam", true, "desc.tmPitTeam");
            _tmAllies = Bind("section.teammates", "cfg.tmAllies", true, "desc.tmAllies");
            _showTeammates = Bind("section.teammates", "cfg.showTeammates", false, "desc.showTeammates");

            ProbePitFireTeam();

            Loc.Lang = _language.Value;
            _appliedLang = Loc.Lang;

            SetupCjkFont();

            _log.LogInfo("[BotCounter] 2.4.2 已加载 / loaded. 语言 Language = " + Loc.Lang);
        }

        private ConfigEntry<T> Bind<T>(string newSection, string keyId, T defaultValue, string descId,
                                       int? min = null, int? max = null)
        {
            string desc = Loc.Get(descId);
            ConfigDescription cd = (min.HasValue && max.HasValue)
                ? new ConfigDescription(desc, new AcceptableValueRange<int>(min.Value, max.Value))
                : new ConfigDescription(desc);

            return Config.Bind(Loc.Get(newSection), Loc.Get(keyId), defaultValue, cd);
        }

        /// <summary>给 IMGUI 挂一个带中文字形的字体；找不到就用默认字体。</summary>
        private void SetupCjkFont()
        {
            try
            {
                string[] candidates =
                {
                    @"C:\Windows\Fonts\msyh.ttc",
                    @"C:\Windows\Fonts\msyh.ttf",
                    @"C:\Windows\Fonts\simhei.ttf",
                    @"C:\Windows\Fonts\Deng.ttf",
                    @"C:\Windows\Fonts\simsun.ttc"
                };

                foreach (string path in candidates)
                {
                    if (!File.Exists(path)) continue;
                    try
                    {
                        var f = Font.CreateDynamicFontFromOSFont(path, 16);
                        if (f != null) { _cjkFont = f; _log.LogInfo("[BotCounter] 中文字体已加载: " + path); return; }
                    }
                    catch { }
                }

                var byName = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei UI", "Microsoft YaHei", "SimHei" }, 16);
                if (byName != null) { _cjkFont = byName; _log.LogInfo("[BotCounter] 中文字体按名称加载成功。"); return; }

                _log.LogWarning("[BotCounter] 未找到中文字体，局内中文可能显示为方块。");
            }
            catch (Exception ex)
            {
                _log.LogWarning("[BotCounter] 中文字体加载失败: " + ex.Message);
            }
        }

        private void Update()
        {
            try
            {
                if (_language != null && _language.Value != _appliedLang)
                {
                    _appliedLang = _language.Value;
                    Loc.Lang = _appliedLang;
                    BuildUIStrings();
                    RelabelBosses();
                    _log.LogInfo("[BotCounter] 语言切换 -> " + Loc.Lang);
                }

                // 第三方 mod 注册了新 Boss → 清掉分类缓存，下一次扫描重新判定
                if (BotCounterApi.ConsumeRegistryChange())
                {
                    _allSeenBots.Clear();
                    _nextUpdate = 0f;
                    RelabelBosses();
                    _log.LogInfo("[BotCounter] 检测到第三方 Boss 注册表变化，已重新分类。当前共 " + Loc.CustomNameCount + " 项。");
                }

                bool botGameInst = Singleton<IBotGame>.Instantiated;
                bool gameWorldInst = Singleton<GameWorld>.Instantiated;
                bool mainPlayerOk = false;
                string mpName = "-";
                try
                {
                    var gw = Singleton<GameWorld>.Instance;
                    if (gw != null)
                    {
                        _gameWorldType = gw.GetType().Name;
                        var mp = gw.MainPlayer;
                        if (mp != null)
                        {
                            mainPlayerOk = true;
                            mpName = mp.Profile != null ? mp.Profile.Nickname : "?";
                        }
                    }
                }
                catch (Exception ex) { _lastError = "WorldProbe: " + ex.GetType().Name + " " + ex.Message; }

                bool currentlyInRaid = botGameInst && gameWorldInst && mainPlayerOk;
                _worldInfo = string.Format("IBotGame={0} GameWorld={1} MainPlayer={2} name={3}", botGameInst, gameWorldInst, mainPlayerOk, mpName);

                if (currentlyInRaid && !_inRaid)
                {
                    _inRaid = true;
                    _allSeenBots.Clear();
                    _deadBotIds.Clear();
                    _teammateIds.Clear();
                    _cTeammates = 0;
                    _nextUpdate = 0f;
                    _nextPitProbe = 0f;
                    ProbePitFireTeam();
                    _log.LogInfo("[BotCounter] 进入战局 RAID ENTERED. " + _worldInfo);
                    _log.LogInfo("[BotCounter] 队友识别状态: " + _teammateProbe + " / 排除队友=" + _excludeTeammates.Value);
                }
                else if (!currentlyInRaid && _inRaid)
                {
                    _inRaid = false;
                    _log.LogInfo("[BotCounter] 离开战局 RAID LEFT.");
                }

                if (_inRaid && _enableMod.Value && Time.time >= _nextUpdate)
                {
                    ScanBots();
                    BuildUIStrings();
                    _nextUpdate = Time.time + _refreshRate.Value;

                    if (_debugLog.Value)
                    {
                        _log.LogInfo(string.Format("[BotCounter] raw={0} alive={1} PMC={2} Scav={3} Rogue={4} Raider={5} Boss={6} 队友={7}(pit={8}/ally={9}/api={10}) err={11}",
                            _rawBotCount, _aliveCount, _cPmc, _cScav, _cRogue, _cRaider, _tBosses.Count,
                            _cTeammates, _tmHitPit, _tmHitAlly, _tmHitApi, _lastError));
                    }
                }
            }
            catch (Exception ex)
            {
                _lastError = "Update: " + ex.GetType().Name + ": " + ex.Message;
                _log.LogError("[BotCounter] Update threw: " + ex);
            }
        }

        // ==================== teammate detection ====================

        /// <summary>
        /// Probe pitFireTeam's teammate registry (pitTeam.Modules.BossPlayers).
        ///
        /// For every teammate it spawns, pitFireTeam:
        ///   1. registers it in _followersByProfileId (key = bot.ProfileId),
        ///      exposed as IsFollowerProfileId(profileId);
        ///   2. records the player squad's BotsGroup in _botsGroup,
        ///      exposed as IsBossGroup(groupId);
        ///   3. calls group.AddAlly(player), i.e. writes the player into that group's Allies.
        ///
        /// Accessed by type name through reflection, so it is a no-op without pitFireTeam.
        /// </summary>
        private void ProbePitFireTeam()
        {
            if (_pitInstanceProp != null) return;
            if (Time.time < _nextPitProbe) return;      // throttle retries, do not scan assemblies every tick
            _nextPitProbe = Time.time + 20f;

            try
            {
                foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    Type t = null;
                    try { t = asm.GetType("pitTeam.Modules.BossPlayers", false); }
                    catch { }

                    if (t == null) continue;

                    PropertyInfo inst = t.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
                    if (inst == null) continue;

                    _pitInstanceProp = inst;
                    _pitIsFollowerProfileId = t.GetMethod("IsFollowerProfileId", new Type[] { typeof(string) });
                    _pitIsBossGroup = t.GetMethod("IsBossGroup", new Type[] { typeof(int) });
                    _teammateProbe = "已找到 pitTeam.Modules.BossPlayers (" + asm.GetName().Name + ")";
                    _log.LogInfo("[BotCounter] 队友识别：已找到 pitFireTeam 队友登记表 (" + asm.GetName().Name + ")");
                    return;
                }

                _teammateProbe = "未找到 pitFireTeam 登记表（未安装或尚未加载）";
            }
            catch (Exception ex)
            {
                _teammateProbe = "探测异常: " + ex.GetType().Name + " " + ex.Message;
            }
        }

        /// <summary>
        /// Is this bot a teammate? reason returns the matching rule for the diagnostic panel.
        /// Order: explicit marker API -> pitFireTeam registry -> player-allied group.
        /// </summary>
        private bool IsTeammateBot(BotOwner bot, Player mainPlayer, out string reason)
        {
            reason = null;

            // pitFireTeam keys its registry by BotOwner.ProfileId (= Profile.Id); keep the same key
            string profileId = null;
            try { profileId = bot.ProfileId; }
            catch { }
            if (string.IsNullOrEmpty(profileId))
            {
                try { if (bot.Profile != null) profileId = bot.Profile.Id; }
                catch { }
            }

            // 1) explicit marker set by another mod through BotCounterApi.MarkTeammate.
            //    Always honoured, independent of the config switches below.
            if (TeammateRegistry.Contains(profileId)) { reason = "标识API"; return true; }

            if (!_excludeTeammates.Value) return false;

            // 2) pitFireTeam registry
            if (_tmPitTeam.Value && IsPitFireTeamTeammate(bot, profileId)) { reason = "pitFireTeam"; return true; }

            // 3) generic rule: this bot's group lists the player as an ally
            if (_tmAllies.Value && IsAlliedWithPlayer(bot, mainPlayer)) { reason = "玩家同盟组"; return true; }

            return false;
        }

        private bool IsPitFireTeamTeammate(BotOwner bot, string profileId)
        {
            if (_pitInstanceProp == null)
            {
                ProbePitFireTeam();
                if (_pitInstanceProp == null) return false;
            }

            try
            {
                object inst = _pitInstanceProp.GetValue(null, null);
                if (inst == null) return false;     // mod present but registry not built yet

                if (_pitIsFollowerProfileId != null && !string.IsNullOrEmpty(profileId))
                {
                    if ((bool)_pitIsFollowerProfileId.Invoke(inst, new object[] { profileId })) return true;
                }

                if (_pitIsBossGroup != null)
                {
                    BotsGroup group = bot.BotsGroup;
                    if (group != null && (bool)_pitIsBossGroup.Invoke(inst, new object[] { group.Id })) return true;
                }
            }
            catch { }

            return false;
        }

        /// <summary>Generic rule: the bot's BotsGroup lists the player in Allies.</summary>
        private static bool IsAlliedWithPlayer(BotOwner bot, Player mainPlayer)
        {
            try
            {
                if (mainPlayer == null) return false;
                BotsGroup group = bot.BotsGroup;
                if (group == null) return false;
                return group.IsAlly(mainPlayer);
            }
            catch { return false; }
        }

        private void ScanBots()        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                _rawBotCount = 0; _aliveCount = 0;
                _roleTally.Clear();

                var botGame = Singleton<IBotGame>.Instance;
                if (botGame == null) { _lastError = "ScanBots: IBotGame.Instance == null"; return; }

                var controller = botGame.BotsController;
                if (controller == null) { _lastError = "ScanBots: BotsController == null"; return; }
                if (controller.Bots == null) { _lastError = "ScanBots: controller.Bots == null"; return; }

                var world = Singleton<GameWorld>.Instance;
                if (world == null || world.MainPlayer == null) { _lastError = "ScanBots: GameWorld/MainPlayer null"; return; }
                Vector3 pPos = world.MainPlayer.Position;
                Player mainPlayer = world.MainPlayer;

                _cPmc = _cScav = _cRogue = _cRaider = 0;
                _cTeammates = 0; _tmHitPit = _tmHitAlly = _tmHitApi = 0;
                _teammateIds.Clear();
                float minPmc = float.MaxValue, minScav = float.MaxValue, minRogue = float.MaxValue, minRaider = float.MaxValue;

                _tBosses.Clear();
                int aliveBosses = 0;

                foreach (var bot in controller.Bots.BotOwners)
                {
                    _rawBotCount++;
                    if (bot == null) continue;

                    string roleStr = "?";
                    int roleValue = 0;
                    try
                    {
                        if (bot.Profile == null) continue;
                        if (bot.Profile.Info == null) continue;
                        if (bot.Profile.Info.Settings == null) continue;
                        var role = bot.Profile.Info.Settings.Role;
                        roleStr = role.ToString();
                        roleValue = (int)role;
                    }
                    catch (Exception ex) { _lastError = "RoleRead: " + ex.GetType().Name + " " + ex.Message; continue; }

                    if (_roleTally.ContainsKey(roleStr)) _roleTally[roleStr]++; else _roleTally[roleStr] = 1;

                    int id = bot.Id;

                    if (!_allSeenBots.TryGetValue(id, out TrackedBotType type))
                    {
                        type = EvaluateBotType(bot);
                        _allSeenBots[id] = type;
                        // 记录最近一次分类结果，便于在诊断面板里核对角色归属
                        _lastClassify = roleStr + " → " + type;
                    }

                    if (bot.HealthController == null || !bot.HealthController.IsAlive)
                    {
                        if (!_deadBotIds.Contains(id)) _deadBotIds.Add(id);
                        continue;
                    }

                    _aliveCount++;

                    // teammate detection: matched bots are skipped entirely
                    string tmReason;
                    if (IsTeammateBot(bot, mainPlayer, out tmReason))
                    {
                        _cTeammates++;
                        _teammateIds.Add(id);
                        if (tmReason == "pitFireTeam") _tmHitPit++;
                        else if (tmReason == "玩家同盟组") _tmHitAlly++;
                        else _tmHitApi++;
                        _lastTeammate = roleStr + " → 队友（" + tmReason + "）";
                        continue;
                    }

                    if (type == TrackedBotType.Ignore) continue;

                    if (type == TrackedBotType.Boss)
                    {
                        aliveBosses++;
                        float bDist = Vector3.Distance(pPos, bot.Transform.position);
                        Color bColor = Color.red;
                        if (_bossHealthColors.Value) bColor = GetHealthColor(GetHealthPercentage(bot));

                        _tBosses.Add(new BossRenderInfo
                        {
                            Text = BossText(roleStr, bot, bDist),
                            Color = bColor,
                            RawRole = roleValue,
                            Distance = bDist,
                            RoleStr = roleStr
                        });
                    }
                    else
                    {
                        float sqrDist = (bot.Transform.position - pPos).sqrMagnitude;
                        if (type == TrackedBotType.Pmc) { _cPmc++; if (sqrDist < minPmc) minPmc = sqrDist; }
                        else if (type == TrackedBotType.Rogue) { _cRogue++; if (sqrDist < minRogue) minRogue = sqrDist; }
                        else if (type == TrackedBotType.Raider) { _cRaider++; if (sqrDist < minRaider) minRaider = sqrDist; }
                        else { _cScav++; if (sqrDist < minScav) minScav = sqrDist; }
                    }
                }

                _distPmc = minPmc != float.MaxValue ? Mathf.Sqrt(minPmc) : -1f;
                _distScav = minScav != float.MaxValue ? Mathf.Sqrt(minScav) : -1f;
                _distRogue = minRogue != float.MaxValue ? Mathf.Sqrt(minRogue) : -1f;
                _distRaider = minRaider != float.MaxValue ? Mathf.Sqrt(minRaider) : -1f;

                _tBossTitle = Loc.BossTitle(aliveBosses);
                _lastError = "none";
            }
            catch (Exception ex)
            {
                _lastError = "ScanBots outer: " + ex.GetType().Name + ": " + ex.Message;
                _log.LogError("[BotCounter] ScanBots threw: " + ex);
            }
            finally
            {
                sw.Stop();
                _scanMs = sw.ElapsedMilliseconds;
            }
        }

        private string BossText(string roleStr, BotOwner bot, float distance)
        {
            string roleLower = roleStr == null ? "" : roleStr.ToLowerInvariant();
            string name = Loc.Boss(roleLower);
            if (name == null)
            {
                string nick = bot.Profile != null ? bot.Profile.Nickname : null;
                if (string.IsNullOrEmpty(nick)) nick = "Unknown Boss";
                name = nick;
                _lastBossResolve = "角色 " + roleStr + " 无译名/未注册 → 用昵称 '" + nick + "'";
            }
            else
            {
                _lastBossResolve = (Loc.IsCustomName(roleLower) ? "角色 " + roleStr + " → 第三方注册名 '" : "角色 " + roleStr + " → 内置译名 '") + name + "'";
            }
            return Loc.BossLabel(name, distance, _showNearest.Value);
        }

        /// <summary>切换语言 / 第三方注册表变化后，重算已缓存的 Boss 文本。</summary>
        private void RelabelBosses()
        {
            for (int i = 0; i < _tBosses.Count; i++)
            {
                var b = _tBosses[i];
                string roleLower = b.RoleStr == null ? "" : b.RoleStr.ToLowerInvariant();
                string name = Loc.Boss(roleLower) ?? "BOSS";
                b.Text = Loc.BossLabel(name, b.Distance, _showNearest.Value);
                _tBosses[i] = b;
            }
        }

        private void BuildUIStrings()
        {
            bool showDist = _showNearest.Value;
            _tPmc = showDist && _distPmc >= 0 ? Loc.CategoryWithDistance("ui.pmc", _distPmc, _cPmc) : Loc.Category("ui.pmc", _cPmc);
            _tScav = showDist && _distScav >= 0 ? Loc.CategoryWithDistance("ui.scav", _distScav, _cScav) : Loc.Category("ui.scav", _cScav);
            _tRogue = showDist && _distRogue >= 0 ? Loc.CategoryWithDistance("ui.rogue", _distRogue, _cRogue) : Loc.Category("ui.rogue", _cRogue);
            _tRaider = showDist && _distRaider >= 0 ? Loc.CategoryWithDistance("ui.raider", _distRaider, _cRaider) : Loc.Category("ui.raider", _cRaider);
            _tTeammate = Loc.Category("ui.teammate", _cTeammates);
        }

        private void ApplyFont(GUIStyle style)
        {
            if (style != null && _cjkFont != null) style.font = _cjkFont;
        }

        private void OnGUI()
        {
            _guiCalls++;
            try
            {
                if (_guiStyle == null)
                {
                    _guiStyle = new GUIStyle { fontStyle = FontStyle.Bold, alignment = TextAnchor.UpperRight };
                    _debugStyle = new GUIStyle { fontStyle = FontStyle.Normal, alignment = TextAnchor.UpperLeft, fontSize = 14 };
                    _debugStyle.normal.textColor = Color.white;
                    _errStyle = new GUIStyle(_debugStyle);
                    _errStyle.normal.textColor = new Color(1f, 0.35f, 0.35f);
                }
                ApplyFont(_guiStyle);
                ApplyFont(_debugStyle);
                ApplyFont(_errStyle);

                if (_debugOverlay != null && _debugOverlay.Value)
                {
                    DrawDebugPanel();
                }

                if (!_inRaid || !_enableMod.Value) return;

                _guiStyle.fontSize = _fontSize.Value;
                float alpha = _opacity.Value / 100f;
                int width = 500;
                int x = Screen.width - _offsetRight.Value - width;
                int y = _offsetTop.Value;

                DrawEntry(x, ref y, width, _tPmc, _showPmc.Value, _cPmc, alpha);
                DrawEntry(x, ref y, width, _tScav, _showScav.Value, _cScav, alpha);
                DrawEntry(x, ref y, width, _tRogue, _showRogue.Value, _cRogue, alpha);
                DrawEntry(x, ref y, width, _tRaider, _showRaider.Value, _cRaider, alpha);
                if (_showTeammates.Value && _cTeammates > 0)
                    DrawEntry(x, ref y, width, _tTeammate, true, _cTeammates, alpha, new Color(0.6f, 0.8f, 1f));

                if (_showBoss.Value && _tBosses.Count > 0)
                {
                    DrawEntry(x, ref y, width, _tBossTitle, true, _tBosses.Count, alpha, Color.red);
                    foreach (var bossInfo in _tBosses)
                    {
                        Color c = bossInfo.Color;
                        c.a = alpha;
                        _guiStyle.normal.textColor = c;
                        GUI.Label(new Rect(x, y, width, 30), bossInfo.Text, _guiStyle);
                        y += _fontSize.Value + 2;
                    }
                }
            }
            catch (Exception ex)
            {
                _lastError = "OnGUI: " + ex.GetType().Name + ": " + ex.Message;
                _log.LogError("[BotCounter] OnGUI threw: " + ex);
            }
        }

        private void DrawDebugPanel()
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== BotCounter 2.4.2 DIAG ===");
            sb.AppendLine("Screen: " + Screen.width + "x" + Screen.height + "  OnGUI: " + _guiCalls + "  Font: " + (_cjkFont != null ? "CJK-OK" : "DEFAULT"));
            sb.AppendLine("EnableMod: " + (_enableMod != null ? _enableMod.Value.ToString() : "?") +
                          "  HideZero: " + (_hideZeroCounts != null ? _hideZeroCounts.Value.ToString() : "?"));
            sb.AppendLine("Lang: " + Loc.Lang + "   第三方注册 Boss: " + Loc.CustomNameCount + " 项");
            sb.AppendLine("_inRaid: " + _inRaid);
            sb.AppendLine(_worldInfo);
            sb.AppendLine("GameWorld type: " + _gameWorldType);
            sb.AppendLine("--- scan (ms=" + _scanMs + ") ---");
            sb.AppendLine("raw BotOwners: " + _rawBotCount + "   alive: " + _aliveCount);
            sb.AppendLine("PMC=" + _cPmc + " Scav=" + _cScav + " Rogue=" + _cRogue + " Raider=" + _cRaider + " Boss=" + _tBosses.Count);
            sb.AppendLine("--- teammates ---");
            sb.AppendLine("excluded: " + _cTeammates + "  (pitFireTeam=" + _tmHitPit + " ally=" + _tmHitAlly + " api=" + _tmHitApi +
                          ")  cfg: master=" + _excludeTeammates.Value + " pit=" + _tmPitTeam.Value + " ally=" + _tmAllies.Value);
            sb.AppendLine("probe: " + _teammateProbe);
            sb.AppendLine("api marks: " + TeammateRegistry.Count + "   last: " + _lastTeammate);
            sb.AppendLine("err: " + _lastError);
            sb.AppendLine("boss: " + _lastBossResolve);
            sb.AppendLine("lastClassify: " + _lastClassify);
            sb.AppendLine("--- roles seen (top 10) ---");

            var roles = new List<KeyValuePair<string, int>>(_roleTally);
            roles.Sort(delegate (KeyValuePair<string, int> a, KeyValuePair<string, int> b) { return b.Value.CompareTo(a.Value); });
            int n = 0;
            foreach (var kv in roles)
            {
                sb.AppendLine("  " + kv.Key + " x" + kv.Value);
                if (++n >= 10) break;
            }
            if (roles.Count == 0) sb.AppendLine("  (none)");

            GUI.color = new Color(0f, 0f, 0f, 0.75f);
            GUI.DrawTexture(new Rect(0, 0, 520, 440), Texture2D.whiteTexture);
            GUI.color = Color.white;

            GUI.Label(new Rect(8, 4, 510, 430), sb.ToString(), (_lastError != "none") ? _errStyle : _debugStyle);
        }

        private void DrawEntry(int x, ref int y, int width, string text, bool enabled, int count, float alpha, Color? overrideColor = null)
        {
            if (!enabled || (_hideZeroCounts.Value && count == 0)) return;
            Color c = overrideColor ?? _globalColor.Value;
            c.a = alpha;
            _guiStyle.normal.textColor = c;
            GUI.Label(new Rect(x, y, width, 30), text, _guiStyle);
            y += _fontSize.Value + 2;
        }

        private TrackedBotType EvaluateBotType(BotOwner bot)
        {
            var role = bot.Profile.Info.Settings.Role;
            string roleStr = role.ToString().ToLowerInvariant();

            // 第三方 mod 注册过的角色 → 一律算 Boss（优先级最高，不受下面任何规则影响）
            if (Loc.IsCustomName(roleStr)) return TrackedBotType.Boss;

            if (bot.GetPlayer != null && !bot.GetPlayer.IsAI)
            {
                return roleStr.Contains("bear") || roleStr.Contains("usec") || roleStr.Contains("pmc")
                    ? TrackedBotType.Pmc
                    : TrackedBotType.Scav;
            }

            if (roleStr.Contains("infected")) return TrackedBotType.Scav;
            if (roleStr.Contains("sectant")) return TrackedBotType.Raider;
            if (role == WildSpawnType.exUsec) return TrackedBotType.Rogue;
            if (role == WildSpawnType.pmcBot || role == WildSpawnType.assaultGroup) return TrackedBotType.Raider;

            // 注意：SPT 4.1.5 的 IsBossOrFollower() 会把 pmcUSEC / pmcBEAR 也判成 true
            // （实测 64 个角色里有 50 个返回 true，其中包含这两个 PMC 角色）。
            // 所以必须先把 PMC 拦下来，否则 PMC 会被算进 Boss，名字还会被当 Boss 翻译。
            if (role == WildSpawnType.pmcUSEC || role == WildSpawnType.pmcBEAR) return TrackedBotType.Pmc;

            if (role.IsBossOrFollower()) return TrackedBotType.Boss;

            if (role == WildSpawnType.assault || role == WildSpawnType.marksman || roleStr.Contains("assault"))
                return TrackedBotType.Scav;

            if (roleStr.Contains("pmc")) return TrackedBotType.Pmc;

            return TrackedBotType.Scav;
        }

        private float GetHealthPercentage(BotOwner bot)
        {
            try
            {
                var healthController = bot.HealthController;
                if (healthController == null) return 100f;

                float current = 0f, max = 0f;
                foreach (EBodyPart part in Enum.GetValues(typeof(EBodyPart)))
                {
                    if (part == EBodyPart.Common) continue;
                    ValueStruct health = healthController.GetBodyPartHealth(part, false);
                    current += health.Current;
                    max += health.Maximum;
                }
                return max > 0 ? (current / max) * 100f : 100f;
            }
            catch { return 100f; }
        }

        private Color GetHealthColor(float percentage)
        {
            if (percentage >= 70f) return new Color(0f, 0.8f, 0f);
            if (percentage >= 40f) return new Color(0.9f, 0.9f, 0f);
            if (percentage >= 10f) return new Color(1f, 0.5f, 0f);
            return new Color(1f, 0f, 0f);
        }
    }
}
