﻿using Psi.Compiler.Intermediate;
using System;
using System.Collections.Generic;
using System.Linq;

using Type = Psi.Compiler.Intermediate.Type;

namespace Psi.Compiler
{
    internal static class Extensions
    {
        public static T NotNull<T>(this T value)
            where T : class
            => NotNull<T>(value, null);

        public static T NotNull<T>(this T value, string name)
            where T : class
        {
            if (value == null)
            {
                if (name != null)
                    throw new ArgumentNullException(name);
                else
                    throw new ArgumentNullException();
            }
            return value;
        }


        public static T? NotNull<T>(this T? value)
            where T : struct
            => NotNull<T>(value, null);

        public static T? NotNull<T>(this T? value, string name)
            where T : struct
        {
            if (value == null)
            {
                if (name != null)
                    throw new ArgumentNullException(name);
                else
                    throw new ArgumentNullException();
            }
            return value;
        }

        public static int IndexOf<T>(this IReadOnlyList<T> list, T value)
        {
            for (int i = 0; i < list.Count; i++)
                if (object.Equals(list[i], value))
                    return i;
            return -1;
        }


        /// <summary>
        /// Permutates a list of options per item into an enumeration of all possible permutations
        /// </summary>
        /// <returns>The permutate.</returns>
        /// <param name="_items">Items.</param>
        /// <typeparam name="T">The 1st type parameter.</typeparam>
        public static IEnumerable<T[]> Permutate<T>(this IEnumerable<T[]> _items)
        {
            var items = _items.ToArray();
            var counters = new int[items.Length];
            var index = 0;
            var last = counters.Length - 1;
            while (counters[last] < items[last].Length)
            {
                yield return items.Select((x, i) => x[counters[i]]).ToArray();

                counters[index]++;
                while (counters[index] >= items[index].Length)
                {
                    if (counters[last] >= items[last].Length)
                        yield break;
                    counters[index] = 0;
                    index++;
                    if (index >= counters.Length)
                        index = 0;
                    counters[index]++;
                }
            }
        }

        public static IEnumerable<T> Append<T>(this IEnumerable<T> src, T next)
        {
            foreach (var item in src)
                yield return item;
            yield return next;
        }

        public static bool TryGetSymbol(this Intermediate.IScope scope, Intermediate.Signature name, out Intermediate.Symbol sym)
        {
            var good = scope.HasSymbol(name);
            if (good)
                sym = scope[name];
            else
                sym = default(Intermediate.Symbol);
            return good;
        }

        public static Intermediate.Symbol FindNamedSymbol(
            this Intermediate.IScope scope, 
            CompoundName name, 
            Intermediate.Type type,
            bool requireConst = false)
        {
            for (int i = 0; i < name.Count - 1; i++)
            {
                if (!scope.TryGetSymbol(new Signature(Type.ModuleType.Signature, name[i]), out Intermediate.Symbol scopeSym))
                    return null;
                if (!scopeSym.IsConst) // don't try to resolve actual module variables!
                    return null;
                scope = scopeSym.GetValue<Intermediate.Module>()?.Symbols;
                if (scope == null)
                    return null;
            }
            // TODO: Implement alias-types here or just use the direct type?
            if (scope.TryGetSymbol(new Signature(type, name.Identifier), out Intermediate.Symbol modSym) == false)
                return null;
            if (requireConst && !modSym.IsConst)
                return null;
            return modSym;
        }
    }
}
