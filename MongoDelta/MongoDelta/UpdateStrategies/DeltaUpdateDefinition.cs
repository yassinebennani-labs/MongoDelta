﻿using System;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Driver;

namespace MongoDelta.UpdateStrategies
{
    class DeltaUpdateDefinition
    {
        private readonly Dictionary<string, ElementUpdate> _elementsToReplace = new Dictionary<string, ElementUpdate>();
        public IReadOnlyCollection<ElementUpdate> ElementsToReplace => Array.AsReadOnly(_elementsToReplace.Values.ToArray());

        private readonly Dictionary<string, ElementIncrement> _elementsToIncrement = new Dictionary<string, ElementIncrement>();
        public IReadOnlyCollection<ElementIncrement> ElementsToIncrement => Array.AsReadOnly(_elementsToIncrement.Values.ToArray());

        public void Set(string elementName, BsonValue value)
        {
            _elementsToReplace.Add(elementName, new ElementUpdate(elementName, value));
        }

        public void Increment(string elementName, BsonValue incrementBy)
        {
            _elementsToIncrement.Add(elementName, new ElementIncrement(elementName, incrementBy));
        }

        public void Merge(string elementNamePrefix, DeltaUpdateDefinition deltaUpdateDefinition)
        {
            foreach (var elementUpdate in deltaUpdateDefinition.ElementsToReplace)
            {
                var newElementName = GetElementNameWithPrefix(elementNamePrefix, elementUpdate.ElementName);
                Set(newElementName, elementUpdate.NewValue);
            }

            foreach (var elementIncrement in deltaUpdateDefinition.ElementsToIncrement)
            {
                var newElementName = GetElementNameWithPrefix(elementNamePrefix, elementIncrement.ElementName);
                Increment(newElementName, elementIncrement.IncrementBy);
            }
        }

        private static string GetElementNameWithPrefix(string prefix, string originalName)
        {
            return prefix + "." + originalName;
        }

        public BsonDocumentUpdateDefinition<T> ToMongoUpdateDefinition<T>()
        {
            var replaceUpdateDefinitions =
                new BsonDocument(ElementsToReplace.Select(e => new BsonElement(e.ElementName, e.NewValue)));

            var incrementUpdateDefinitions =
                new BsonDocument(ElementsToIncrement.Select(e => new BsonElement(e.ElementName, e.IncrementBy)));

            var mongoUpdateDefinition = new BsonDocument();
            if (replaceUpdateDefinitions.Any())
            {
                mongoUpdateDefinition.Add("$set", replaceUpdateDefinitions);
            }

            if (incrementUpdateDefinitions.Any())
            {
                mongoUpdateDefinition.Add("$inc", incrementUpdateDefinitions);

            }

            return new BsonDocumentUpdateDefinition<T>(mongoUpdateDefinition);
        }

        public class ElementUpdate
        {
            public string ElementName { get; }
            public BsonValue NewValue { get; }

            public ElementUpdate(string elementName, BsonValue newValue)
            {
                ElementName = elementName;
                NewValue = newValue;
            }
        }

        public class ElementIncrement
        {
            public string ElementName { get; }
            public BsonValue IncrementBy { get; }

            public ElementIncrement(string elementName, BsonValue incrementBy)
            {
                ElementName = elementName;
                IncrementBy = incrementBy;
            }
        }
    }
}
