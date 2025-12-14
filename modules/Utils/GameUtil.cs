using System;
using System.Reflection;
using UnityEngine;

namespace bepinex_test.Modules.Utils
{
    public static class GameUtil
    {
        static object _target;
        static MemberInfo _member;
        static bool _initialized;

        static bool NameMatches(string n)
        {
            var s = n.ToLowerInvariant();
            return s == "currency" || s == "money" || s == "bucks";
        }

        public static bool RefreshMapping()
        {
            var holder = FindCurrencyHolder();
            _target = holder.target;
            _member = holder.member;
            _initialized = true;
            return _target != null && _member != null;
        }

        static void Ensure()
        {
            if (_initialized) return;
            RefreshMapping();
        }

        public static int? TryGetCurrency()
        {
            Ensure();
            if (_target == null || _member == null) return null;
            if (_member is FieldInfo fi)
            {
                var v = fi.GetValue(_target);
                if (v is int i) return i;
                return null;
            }
            if (_member is PropertyInfo pi)
            {
                var v = pi.GetValue(_target, null);
                if (v is int i) return i;
                return null;
            }
            return null;
        }

        public static bool TrySetCurrency(int value)
        {
            Ensure();
            if (_target == null || _member == null) return false;
            if (_member is FieldInfo fi)
            {
                fi.SetValue(_target, value);
                return true;
            }
            if (_member is PropertyInfo pi && pi.CanWrite)
            {
                pi.SetValue(_target, value, null);
                return true;
            }
            return false;
        }

        static (object target, MemberInfo member) FindCurrencyHolder()
        {
            var objs = Resources.FindObjectsOfTypeAll(typeof(MonoBehaviour));
            foreach (var o in objs)
            {
                var mb = o as MonoBehaviour;
                if (mb == null) continue;
                var type = mb.GetType();
                var flds = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                foreach (var f in flds)
                {
                    if (NameMatches(f.Name) && f.FieldType == typeof(int))
                    {
                        return (mb, f);
                    }
                    if (f.FieldType != null && !f.FieldType.IsPrimitive)
                    {
                        var inner = f.GetValue(mb);
                        if (inner == null) continue;
                        var innerType = inner.GetType();
                        var innerFlds = innerType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        foreach (var inf in innerFlds)
                        {
                            if (NameMatches(inf.Name) && inf.FieldType == typeof(int))
                            {
                                return (inner, inf);
                            }
                        }
                        var innerProps = innerType.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        foreach (var inp in innerProps)
                        {
                            if (NameMatches(inp.Name) && inp.PropertyType == typeof(int))
                            {
                                return (inner, inp);
                            }
                        }
                    }
                }
                var props = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                foreach (var p in props)
                {
                    if (NameMatches(p.Name) && p.PropertyType == typeof(int))
                    {
                        return (mb, p);
                    }
                }
            }
            return (null, null);
        }
    }
}
