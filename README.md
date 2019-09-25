# MongoDelta

![Azure DevOps builds (branch)](https://img.shields.io/azure-devops/build/adrianbradfield/df1cab1e-21bf-4a8e-a335-29d7a5b730ab/8/master?style=for-the-badge) ![Azure DevOps tests (compact)](https://img.shields.io/azure-devops/tests/adrianbradfield/df1cab1e-21bf-4a8e-a335-29d7a5b730ab/8/master?compact_message&style=for-the-badge) ![Azure DevOps coverage (branch)](https://img.shields.io/azure-devops/coverage/adrianbradfield/df1cab1e-21bf-4a8e-a335-29d7a5b730ab/8/master?style=for-the-badge) ![Nuget](https://img.shields.io/nuget/v/MongoDelta?style=for-the-badge) ![Nuget](https://img.shields.io/nuget/dt/MongoDelta?style=for-the-badge)

Implements a UnitOfWork pattern with change tracking for the C# MongoDb driver

To use this library, you can install it into your project from [Nuget](https://www.nuget.org/packages/MongoDelta/)

## The aims of this library are:
- Create a UnitOfWork base class with change tracking, for use with MongoDb
- Allow domain models to be incrementally updated (i.e. only properties that have changed should be updated)
- Integrate as closely as possible with the MongoDb C# driver for mapping
- Require no implementation leakage (i.e. data access logic) into the domain model

## Roadmap:
- [x] *V1.0* - UnitofWork pattern with change tracking
- [ ] *V1.1* - Integrate with ASP.NET Core for proper dependency injection behavior for the UnitOfWork
- [ ] *V1.2* - Incremental updates for the root document
- [ ] *V1.3* - Incremental updates for sub-documents and numeric fields
- [ ] *V1.4* - Incremental updates for collection fields

## Getting Started
For this walkthrough, I will just be using a simple UserAggregate class for my domain model

```cs
class UserAggregate
{
    public Guid Id { get; set; }
    public string FirstName { get; set; }
    public string Surname { get; set; }

    private UserAggregate(){}

    public UserAggregate(string firstName, string surname)
    {
        FirstName = firstName;
        Surname = surname;
    }
}
```

Note that the parameterless private constructor is required for the MongoDb driver to create an instance of it

### Creating the UnitOfWork
To create a UnitOfWork that can persist this aggregate, you need to create a new class that extends from UnitOfWorkBase
```cs
class UserUnitOfWork : UnitOfWorkBase
{
    public UserUnitOfWork(IMongoDatabase database) : base(database, useTransactions: false)
    {
        RegisterRepository<UserAggregate>("users");
    }
    public MongoDeltaRepository<UserAggregate> Users => GetRepository<UserAggregate>();
}
```
The constructor for the base class requires an instance of IMongoDatabase. There is also an optional parameter that sets whether multi-document transactions should be used which defaults to true.

You can tell the unit of work to generate a repository for you by using the `RegisterRepository` method, passing in the model type, and the name of the collection to use for that type.

To access the repository outside the unit of work, you can create a get only property which calls the `GetRepository` method.

### Querying for documents
On the repository, there are 2 methods for querying:
- `QueryAsync` - Returns a readonly collection of models that match the filter
- `QuerySingleAsync` - Returns a single model that matches the filter or null. If there is more than one model that matches the filter, it will throw an exception

```cs
async Task QueryExample(Guid id){
  var unitOfWork = new UnitOfWork(_database);
  var allBobs = await unitOfWork.Users.QueryAsync(query => query.Where(u => u.FirstName == "Bob"));
  var singleUser = await unitOfWork.Users.QuerySingleAsync(query => query.Where(u => u.Id == id));
}
```

### Adding a document
The repository also has an `Add` method that can be used to store a new model
```cs
async Task<Guid> AddExample()
{
  var unitOfWork = new UnitOfWork(_database);
  var user = new UserAggregate("John", "Smith");
  
  unitOfWork.Users.Add(user);
  await unitOfWork.CommitAsync();
  
  return user.Id;
}
```
Here we have added the user to the repository and committed the UnitOfWork. The users `Id` property has been automatically populated by the MongoDb driver

### Updating a document
```cs
async Task UpdateExample(Guid id)
{
  var unitOfWork = new UnitOfWork(_database);
  var user = await unitOfWork.Users.QuerySingleAsync(query => query.Where(u => u.Id == id));
  
  user.FirstName = "Joan";
  await unitOfWork.CommitAsync();
}
```
Here MongoDelta has started tracking changes to the user model once it has been queried from the repository. When the `CommitAsync` method is called on the UnitOfWork, it will detect the user as being changed, and update it.

The change tracking works with the mapping set for the MongoDb driver. If you are mapping to backing fields, these changes will be picked up. Similarly if you aren't tracking a property at all, a change to that property won't cause the document to be updated.

### Removing a document
Removing a document works much the same way as adding one
```cs
async Task RemoveExample(Guid id)
{
  var unitOfWork = new UnitOfWork(_database);
  var user = await unitOfWork.Users.QuerySingleAsync(query => query.Where(u => u.Id == id));
  
  unitOfWork.Users.Remove(user);
  await unitOfWork.CommitAsync();
}
```
The document you are removing must have been queried from a repository belonging to the same unit of work, otherwise it will throw an exception
