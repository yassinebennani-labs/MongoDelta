﻿using System.Threading.Tasks;
using MongoDelta.IntegrationTests.Models;
using NUnit.Framework;

namespace MongoDelta.IntegrationTests
{
    [TestFixture]
    public class RemoveRecordTests : MongoTestBase
    {
        [Test]
        public async Task AddAndRemove_SimpleObject_Success()
        {
            var testUser = await UserAggregate.AddTestUser(Database, CollectionName);

            var unitOfWork = new UserUnitOfWork(Database, CollectionName);
            var model = await unitOfWork.Users.QuerySingleAsync(user => user.Id == testUser.Id);
            unitOfWork.Users.Remove(model);
            await unitOfWork.CommitAsync();

            var removeQueryResult = await unitOfWork.Users.QuerySingleAsync(user => user.FirstName == "John");
            Assert.IsNull(removeQueryResult);
        }
    }
}