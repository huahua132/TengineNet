#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace BehaviorTree.Editor
{
    /// <summary>
    /// 节点类型信息
    /// </summary>
    public class BehaviorNodeTypeInfo
    {
        public Type Type { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public BehaviorProcessType ProcessType { get; set; }
        public Color Color { get; set; }
        public string Icon { get; set; }
    }

    /// <summary>
    /// 行为树节点注册表 - 自动发现所有带有BehaviorProcessNodeAttribute的节点
    /// </summary>
    public static class BehaviorNodeRegistry
    {
        private static List<BehaviorNodeTypeInfo> _allNodes;
        private static Dictionary<BehaviorProcessType, List<BehaviorNodeTypeInfo>> _nodesByType;
        private static Dictionary<BehaviorProcessType, Color> _typeColors;
        private static Dictionary<BehaviorProcessType, string> _typeIcons;

        static BehaviorNodeRegistry()
        {
            InitializeColors();
            InitializeIcons();
            DiscoverNodes();
        }

        /// <summary>
        /// 初始化类型颜色
        /// </summary>
        private static void InitializeColors()
        {
            _typeColors = new Dictionary<BehaviorProcessType, Color>
            {
                { BehaviorProcessType.composite, new Color(0.4f, 0.6f, 0.9f) },    // 蓝色 - 组合节点
                { BehaviorProcessType.decorator, new Color(0.9f, 0.6f, 0.4f) },    // 橙色 - 装饰节点
                { BehaviorProcessType.condition, new Color(0.9f, 0.9f, 0.4f) },    // 黄色 - 条件节点
                { BehaviorProcessType.action, new Color(0.4f, 0.9f, 0.6f) }        // 绿色 - 行为节点
            };
        }

        /// <summary>
        /// 初始化类型图标（使用Unity内置图标）
        /// </summary>
        private static void InitializeIcons()
        {
            _typeIcons = new Dictionary<BehaviorProcessType, string>
            {
                { BehaviorProcessType.composite, "d_Folder Icon" },
                { BehaviorProcessType.decorator, "d_editicon.sml" },
                { BehaviorProcessType.condition, "d_P4_CheckOutLocal" },
                { BehaviorProcessType.action, "d_Animation.Play" }
            };
        }

        /// <summary>
        /// 发现所有节点类型
        /// </summary>
        private static void DiscoverNodes()
        {
            _allNodes = new List<BehaviorNodeTypeInfo>();
            _nodesByType = new Dictionary<BehaviorProcessType, List<BehaviorNodeTypeInfo>>();

            // 初始化分类列表
            foreach (BehaviorProcessType type in Enum.GetValues(typeof(BehaviorProcessType)))
            {
                _nodesByType[type] = new List<BehaviorNodeTypeInfo>();
            }

            // 查找所有程序集
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies)
            {
                try
                {
                    // 查找所有继承自BehaviorProcessNodeBase的类型
                    var types = assembly.GetTypes()
                        .Where(t => t.IsClass && !t.IsAbstract && typeof(BehaviorProcessNodeBase).IsAssignableFrom(t));

                    foreach (var type in types)
                    {
                        // 获取BehaviorProcessNodeAttribute
                        var attribute = type.GetCustomAttribute<BehaviorProcessNodeAttribute>();
                        if (attribute != null)
                        {
                            var nodeInfo = new BehaviorNodeTypeInfo
                            {
                                Type = type,
                                Name = attribute.Name,
                                Description = attribute.Desc,
                                ProcessType = attribute.Type,
                                Color = _typeColors[attribute.Type],
                                Icon = _typeIcons[attribute.Type]
                            };

                            _allNodes.Add(nodeInfo);
                            _nodesByType[attribute.Type].Add(nodeInfo);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Failed to load types from assembly {assembly.FullName}: {ex.Message}");
                }
            }

            Debug.Log($"BehaviorNodeRegistry: Discovered {_allNodes.Count} node types");
        }

        /// <summary>
        /// 获取所有节点
        /// </summary>
        public static List<BehaviorNodeTypeInfo> GetAllNodes()
        {
            if (_allNodes == null)
            {
                DiscoverNodes();
            }
            return _allNodes;
        }

        /// <summary>
        /// 按类型获取节点
        /// </summary>
        public static List<BehaviorNodeTypeInfo> GetNodesByType(BehaviorProcessType type)
        {
            if (_nodesByType == null)
            {
                DiscoverNodes();
            }
            return _nodesByType.ContainsKey(type) ? _nodesByType[type] : new List<BehaviorNodeTypeInfo>();
        }

        /// <summary>
        /// 根据类型名获取节点信息
        /// </summary>
        public static BehaviorNodeTypeInfo GetNodeInfo(string typeName)
        {
            if (_allNodes == null)
            {
                DiscoverNodes();
            }
            return _allNodes.FirstOrDefault(n => n.Type.Name == typeName);
        }

        /// <summary>
        /// 获取类型颜色
        /// </summary>
        public static Color GetTypeColor(BehaviorProcessType type)
        {
            return _typeColors.ContainsKey(type) ? _typeColors[type] : Color.gray;
        }

        /// <summary>
        /// 获取类型图标
        /// </summary>
        public static string GetTypeIcon(BehaviorProcessType type)
        {
            return _typeIcons.ContainsKey(type) ? _typeIcons[type] : "GameObject Icon";
        }

        /// <summary>
        /// 刷新节点注册表
        /// </summary>
        public static void Refresh()
        {
            DiscoverNodes();
        }
    }
}
#endif