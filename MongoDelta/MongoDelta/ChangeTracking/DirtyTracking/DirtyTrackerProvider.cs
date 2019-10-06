﻿using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson.Serialization;
using MongoDelta.Mapping;

namespace MongoDelta.ChangeTracking.DirtyTracking
{
    static class DirtyTrackerProvider
    {
        public static IMemberDirtyTrackerTemplate GetTrackerTemplateForMember(object aggregate, BsonMemberMap map)
        {
            var classMap = BsonClassMap.LookupClassMap(map.MemberType);
            var updateConfig = classMap.GetDeltaUpdateConfiguration();
            if (updateConfig.UseDeltaUpdateStrategy)
            {
                return new SubObjectDirtyTrackerTemplate(aggregate, map);
            }

            return new MemberDirtyTrackerTemplate(aggregate, map);
        }

        public static IEnumerable<IMemberDirtyTracker> ToTrackers(this IEnumerable<IMemberDirtyTrackerTemplate> templates,
            object aggregate)
        {
            return templates.Select(t => t.ToDirtyTracker(aggregate));
        }
    }
}