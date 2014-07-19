using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Data.Entity;
using System.Data.Entity.Core.Objects.DataClasses;

namespace EntityFramework.CollectionSemantics
{
    public class RemoveSemantics<TParent> where TParent : class
    {
        private readonly DbContext _context;

        private RemoveSemantics(DbContext context)
        {
            _context = context;
        }

        public static RemoveSemantics<TParent> ForContext(DbContext context)
        {
            return new RemoveSemantics<TParent>(context);
        }


        public void DeleteOnRemove<TChild>(Func<TParent, ICollection<TChild>> property) where TChild : class
        {
            _context.Set<TParent>().Local.CollectionChanged += (sender, e) =>
            {
                if (e.Action != NotifyCollectionChangedAction.Add)
                {
                    return;
                }

                foreach (TParent entity in e.NewItems)
                {
                    SubscribeProperty(entity, property);
                }
            };
        }

        private void SubscribeProperty<TChild>(TParent incident, Func<TParent, ICollection<TChild>> selector) where TChild : class
        {
            var entities = selector(incident) as EntityCollection<TChild>;

            if (entities != null)
            {
                entities.AssociationChanged += (sender, e) =>
                {
                    if (e.Action == CollectionChangeAction.Remove)
                    {
                        var entity = e.Element as TChild;
                        if (entity != null)
                        {
                            _context.Entry(entity).State = EntityState.Deleted;
                        }
                    }
                };
            }
        }
    }
}
