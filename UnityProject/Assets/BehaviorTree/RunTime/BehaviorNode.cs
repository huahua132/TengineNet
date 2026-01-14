
using System.Collections.Generic;
using System;
using TEngine;

namespace BehaviorTree
{
    public interface IBehaviorNode
    {
        public int ID {get;}
        public List<IBehaviorNode> Childrens {get;}
        public BehaviorRet TickRun();
        public BehaviorRet Yield();
        public bool IsResume();
    }

    //行为树结点
    public class BehaviorNode: IBehaviorNode, IMemory
    {
        public int ID {get; private set;}                                          //结点ID
        List<IBehaviorNode> IBehaviorNode.Childrens => Childrens;
        public List<IBehaviorNode> Childrens = new();                              //子节点
        public BehaviorProcessNodeBase ProcessNode {get; private set;}             //执行结点
        private bool _isYield  = false;                                            //是否挂起
        private bool _hasStarted = false;                                          //是否已经执行过OnStart
        private IBehaviorContext _context;

        public void Clear()
        {
            ID = 0;
            _isYield = false;
            _hasStarted = false;
            if (ProcessNode != null)
            {
                MemoryPool.Release(ProcessNode);
                ProcessNode = null;
            }
            for (int i = Childrens.Count - 1; i >= 0; i--)
            {
                var node = Childrens[i];
                MemoryPool.Release((BehaviorNode)node);
            }
            Childrens.Clear();
        }

        public void Init(int id, Type processType, IBehaviorContext context, BehaviorNodeData nodeData = null)
        {
            ID = id;
            _context = context;
            ProcessNode = (BehaviorProcessNodeBase)MemoryPool.Acquire(processType);
            ProcessNode.Create(this, _context);
            
            // 加载参数值
            if (nodeData != null && nodeData.parametersList != null && nodeData.parametersList.Count > 0)
            {
                LoadParameters(nodeData);
            }
        }
        
        /// <summary>
        /// 从节点数据加载参数到ProcessNode的字段
        /// </summary>
        private void LoadParameters(BehaviorNodeData nodeData)
        {
            var fields = ProcessNode.GetType().GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            
            foreach (var field in fields)
            {
                string fieldName = field.Name;
                if (nodeData.HasParameter(fieldName))
                {
                    string valueStr = nodeData.GetParameter(fieldName);
                    try
                    {
                        object value = ConvertValue(valueStr, field.FieldType);
                        field.SetValue(ProcessNode, value);
                    }
                    catch (System.Exception ex)
                    {
                        UnityEngine.Debug.LogError($"Failed to set parameter {fieldName} = {valueStr}: {ex.Message}");
                    }
                }
            }
        }
        
        /// <summary>
        /// 将字符串值转换为目标类型
        /// </summary>
        private object ConvertValue(string valueStr, Type targetType)
        {
            if (targetType == typeof(string))
                return valueStr;
            
            if (targetType == typeof(int))
                return int.Parse(valueStr);
            
            if (targetType == typeof(float))
                return float.Parse(valueStr);
            
            if (targetType == typeof(double))
                return double.Parse(valueStr);
            
            if (targetType == typeof(bool))
                return bool.Parse(valueStr);
            
            if (targetType == typeof(long))
                return long.Parse(valueStr);
            
            if (targetType.IsEnum)
                return System.Enum.Parse(targetType, valueStr);
            
            // 默认尝试使用Convert
            return System.Convert.ChangeType(valueStr, targetType);
        }

        public BehaviorRet TickRun()
        {
            if (_context.IsAbort()) return BehaviorRet.RUNNING;

            if (!_isYield)
            {
                _context.PushStackNode(this);
                
                // 首次执行，调用OnStart（整个生命周期只调用一次）
                if (!_hasStarted)
                {
                    _hasStarted = true;
                    ProcessNode.OnStart();
                }
            }
            
            var ret = ProcessNode.OnTickRun();
            if (ret == BehaviorRet.ABORT)
            {
                _context.SetAbort();
                return BehaviorRet.RUNNING;
            }

            if (ret != BehaviorRet.RUNNING)
            {
                _isYield = false;
                // 节点完成，不重置_hasStarted（整个生命周期只执行一次OnStart）
                _context.PopStackNode();
            }
            else
            {
                _isYield = true;
            }

            _context.SetLastRet(ret);
            return ret;
        }

        public void AddChild(BehaviorNode node)
        {
            Childrens.Add(node);
        }

        public BehaviorRet Yield()
        {
            _isYield = true;
            return BehaviorRet.RUNNING;
        }

        public bool IsResume()
        {
            return _isYield;
        }
    }
}