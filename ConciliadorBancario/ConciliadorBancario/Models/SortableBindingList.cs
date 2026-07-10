using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace ConciliadorBancario.Models
{
    public class SortableBindingList<T> : BindingList<T>
    {
        private bool _isSorted;
        private ListSortDirection _sortDirection;
        private PropertyDescriptor _sortProperty;

        public SortableBindingList() { }
        public SortableBindingList(IList<T> list) : base(list) { }

        protected override bool SupportsSortingCore => true;
        protected override bool IsSortedCore => _isSorted;
        protected override PropertyDescriptor SortPropertyCore => _sortProperty;
        protected override ListSortDirection SortDirectionCore => _sortDirection;

        protected override void ApplySortCore(PropertyDescriptor prop, ListSortDirection direction)
        {
            var items = this.Items as List<T>;
            if (items != null)
            {
                var query = items.AsQueryable();
                if (direction == ListSortDirection.Ascending)
                    query = query.OrderBy(i => prop.GetValue(i));
                else
                    query = query.OrderByDescending(i => prop.GetValue(i));

                int index = 0;
                foreach (var item in query)
                {
                    this.Items[index] = item;
                    index++;
                }

                _isSorted = true;
                _sortDirection = direction;
                _sortProperty = prop;
                this.OnListChanged(new ListChangedEventArgs(ListChangedType.Reset, -1));
            }
        }

        protected override void RemoveSortCore()
        {
            _isSorted = false;
            _sortDirection = base.SortDirectionCore;
            _sortProperty = base.SortPropertyCore;
            this.OnListChanged(new ListChangedEventArgs(ListChangedType.Reset, -1));
        }
    }
}
