ef-collectionsemantics
======================

Entity Framework has a restriction that doesn't allow you to remove a child element from a collection, and must remove the 
entity from it's DbSet<> instead.


### Example

In the following models, we define a 1-to-many relationship between a user and their orders. A user can have multiple orders, 
but an order can only belong to a single user. It's important in this example (and for the purpose of this library), that 
the ParentId foreign key be non-nullable. 

````csharp

public class User
{
    public virtual int UserId { get; set; }
    
    public virtual ICollection<Order> Orders { get; set; }
}

public class Order
{
    public virtual int ChildId { get; set; }
    
    public virtual int OrderId { get; set; }
    
    public virtual User User { get; set; }
}
````

If you attempt to remove an order from the user directly, Entity Framework will throw an exception. 


````csharp

public void DeleteLastOrder(int userId)
{
    using (var db = new TestDbContext())
    {
        User user = db.Users.Include(u => u.Orders).Find(userId);
        
        var lastOrder = user.Orders.OrderByDescending(o => o.OrderDate).First();
        
        // do something with the order, maybe log a message
        
        user.Orders.Remove(lastOrder);
        
        db.SaveChanges(); // throws exception
    }
}

````

calling SaveChanges() will throw:

> System.InvalidOperationException: The operation failed: The relationship could not be changed because one or more of the foreign-key properties is non-nullable. When a change is made to a relationship, the related foreign-key property is set to a null value. If the foreign-key does not support null values, a new relationship must be defined, the foreign-key property must be assigned another non-null value, or the unrelated object must be deleted.

For a detailed explanation of why, see [this post](http://blog.oneunicorn.com/2012/06/02/deleting-orphans-with-entity-framework/

### Delete on Remove Semantics

In looking for a workaround, I found this great solution by [brockallen](http://brockallen.com/2014/03/30/how-i-made-ef-work-more-like-an-object-database/)

It works by hooking into the collection's Remove() event, and automatically marking removed entities as Deleted. Since I've used this in multiple projects, I've decided to write a generic implementation. 

#### How to Use

Inside your DbContext's constructor:

````csharp

public class YourDbContext : DbContext
{
    public YourDbContext()
    {
        RemoveSemantics<User>.ForContext(this).DeleteOnRemove(i => i.Orders);
    }
}

````

Now removing a child entity from a collection should work, and delete the corresponding row in the database. 

````csharp

public void DeleteLastOrder(int userId)
{
    using (var db = new TestDbContext())
    {
        User user = db.Users.Include(u => u.Orders).Find(userId);
        
        var lastOrder = user.Orders.OrderByDescending(o => o.OrderDate).First();
        
        user.Orders.Remove(lastOrder);
        
        db.SaveChanges(); // this works now
    }
}

````
