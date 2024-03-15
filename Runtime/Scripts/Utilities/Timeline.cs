//Copyright (c) 2024 Jefferson University (James Bruska)
//Copyright (c) 2024 Bruska Technologies LLC (James Bruska)
//Copyright (c) 2023 University of Pennsylvania

//This file is part of UnityEPL.
//UnityEPL is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
//UnityEPL is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
//You should have received a copy of the GNU General Public License along with UnityEPL. If not, see <https://www.gnu.org/licenses/>. 

using System;
using System.Collections;
using System.Collections.Generic;

namespace UnityEPL {

    [Serializable]
    public class Timeline<T> : IList<T> {
        protected List<T> items = new List<T>();
        protected bool reset_on_load;
        public virtual bool IsReadOnly { get { return false; } }
        public int index;
        public virtual int Count { get { return items.Count; } }

        public Timeline(IEnumerable<T> states,
                        bool reset_on_load = false) {
            this.AddRange(states);
            this.reset_on_load = reset_on_load;
        }

        public Timeline(bool reset_on_load = false) {
            this.reset_on_load = reset_on_load;
        }

        virtual public bool IncrementState() {
            if (index < this.Count - 1) {
                index++;
                return true;
            } else {
                return false;
            }
        }

        virtual public bool DecrementState() {
            if (index > 0) {
                index--;
                return true;
            } else {
                return false;
            }
        }

        public virtual T this[int i] {
            get { return items[i]; }
            set { throw new NotSupportedException("Indexing is read only"); }
        }

        public T GetState() {
            return this[index];
        }

        virtual public int IndexOf(T item) {
            throw new NotSupportedException("Provided only for compatibility");
        }

        virtual public void Insert(int index, T item) {
            items.Insert(index, item);
        }

        virtual public void RemoveAt(int index) {
            items.RemoveAt(index);
        }

        virtual public void Add(T item) {
            items.Add(item);
        }

        virtual public void AddRange(IEnumerable<T> new_items) {
            items.AddRange(new_items);
        }

        virtual public void Clear() {
            items.Clear();
        }

        virtual public bool Contains(T item) {
            throw new NotSupportedException("Provided only for compatibility");
        }

        virtual public void CopyTo(T[] array, int index) {
            items.CopyTo(array, index);
        }

        virtual public bool Remove(T item) {
            throw new NotSupportedException("Provided only for compatibility");
        }

        virtual public IEnumerator<T> GetEnumerator() {
            return items.GetEnumerator();
        }

        IEnumerator System.Collections.IEnumerable.GetEnumerator() {
            return this.GetEnumerator();
        }
    }

}