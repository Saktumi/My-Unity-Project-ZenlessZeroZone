using System;
using UnityEngine;

/// <summary>
/// 标记字符串数组字段（如状态列表），在 Inspector 中提示应填写的类型名。
/// </summary>
public class ClassTypeName : PropertyAttribute
{
    public Type type;

    public ClassTypeName(Type type)
    {
        this.type = type;
    }
}
