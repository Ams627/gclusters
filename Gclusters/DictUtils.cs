// Copyright (c) Adrian Sims 2018
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
namespace Gclusters
{
    using System.Collections.Generic;

    internal static class DictUtils
    {
        /// <summary>
        /// Add an entry to a List contained within a Dictionary. Add a new List containing the item if there isn't already one in the Dictionary.
        /// </summary>
        /// <typeparam name="T1">Type of the Dictionary key</typeparam>
        /// <typeparam name="T2">Type of the data stored in the List</typeparam>
        /// <param name="d">Dictionary to add to</param>
        /// <param name="key">Dictionary key of the List to add to</param>
        /// <param name="listEntry">Value to add to the List</param>
        public static void AddEntryToList<T1, T2>(Dictionary<T1, List<T2>> d, T1 key, T2 listEntry)
        {
            if (!d.TryGetValue(key, out var list))
            {
                list = new List<T2>();
                d.Add(key, list);
            }

            list.Add(listEntry);
        }
    }
}