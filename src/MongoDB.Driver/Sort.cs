﻿/* Copyright 2010-2014 MongoDB Inc.
*
* Licensed under the Apache License, Version 2.0 (the "License");
* you may not use this file except in compliance with the License.
* You may obtain a copy of the License at
*
* http://www.apache.org/licenses/LICENSE-2.0
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing permissions and
* limitations under the License.
*/

using System;
using System.Linq.Expressions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Builders;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Linq.Utils;

namespace MongoDB.Driver
{
    /// <summary>
    /// Base class for sorts.
    /// </summary>
    /// <typeparam name="TDocument">The type of the document.</typeparam>
    public abstract class Sort<TDocument>
    {
        /// <summary>
        /// Renders the sort to a <see cref="BsonDocument"/>.
        /// </summary>
        /// <param name="documentSerializer">The document serializer.</param>
        /// <param name="serializerRegistry">The serializer registry.</param>
        /// <returns>A <see cref="BsonDocument"/>.</returns>
        public abstract BsonDocument Render(IBsonSerializer<TDocument> documentSerializer, IBsonSerializerRegistry serializerRegistry);

        /// <summary>
        /// Performs an implicit conversion from <see cref="BsonDocument"/> to <see cref="Sort{TDocument}"/>.
        /// </summary>
        /// <param name="document">The document.</param>
        /// <returns>
        /// The result of the conversion.
        /// </returns>
        public static implicit operator Sort<TDocument>(BsonDocument document)
        {
            return new BsonDocumentSort<TDocument>(document);
        }

        /// <summary>
        /// Performs an implicit conversion from <see cref="System.String"/> to <see cref="Sort{TDocument}"/>.
        /// </summary>
        /// <param name="json">The json.</param>
        /// <returns>
        /// The result of the conversion.
        /// </returns>
        public static implicit operator Sort<TDocument>(string json)
        {
            return new JsonSort<TDocument>(json);
        }
    }

    /// <summary>
    /// A <see cref="BsonDocument"/> based sort.
    /// </summary>
    /// <typeparam name="TDocument">The type of the document.</typeparam>
    public sealed class BsonDocumentSort<TDocument> : Sort<TDocument>
    {
        private readonly BsonDocument _document;

        /// <summary>
        /// Initializes a new instance of the <see cref="BsonDocumentSort{TDocument}"/> class.
        /// </summary>
        /// <param name="document">The document.</param>
        public BsonDocumentSort(BsonDocument document)
        {
            _document = Ensure.IsNotNull(document, "document");
        }

        /// <summary>
        /// Gets the document.
        /// </summary>
        public BsonDocument Document
        {
            get { return _document; }
        }

        /// <inheritdoc />
        public override BsonDocument Render(IBsonSerializer<TDocument> documentSerializer, IBsonSerializerRegistry serializerRegistry)
        {
            return _document;
        }
    }

    /// <summary>
    /// An <see cref="Expression"/> based sort.
    /// </summary>
    /// <typeparam name="TDocument">The type of the document.</typeparam>
    public sealed class ExpressionSort<TDocument> : Sort<TDocument>
    {
        private readonly Direction _direction;
        private readonly Expression<Func<TDocument, object>> _expression;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExpressionSort{TDocument}" /> class.
        /// </summary>
        /// <param name="expression">The expression.</param>
        /// <param name="direction">The direction.</param>
        public ExpressionSort(Expression<Func<TDocument, object>> expression, Direction direction = Direction.Ascending)
        {
            _expression = Ensure.IsNotNull(expression, "expression");
            _direction = direction;
        }

        /// <summary>
        /// Gets the expression.
        /// </summary>
        public Expression<Func<TDocument, object>> Expression
        {
            get { return _expression; }
        }

        /// <inheritdoc />
        public override BsonDocument Render(IBsonSerializer<TDocument> documentSerializer, IBsonSerializerRegistry serializerRegistry)
        {
            var helper = new BsonSerializationInfoHelper();
            helper.RegisterExpressionSerializer(_expression.Parameters[0], documentSerializer);
            var sortByBuilder = new SortByBuilder<TDocument>(helper);
            switch (_direction)
            {
                case Direction.Ascending:
                    sortByBuilder = sortByBuilder.Ascending(_expression);
                    break;
                case Direction.Descending:
                    sortByBuilder = sortByBuilder.Descending(_expression);
                    break;
                default:
                    throw new MongoInternalException("Invalid Direction.");
            }
            var serializer = serializerRegistry.GetSerializer<IMongoSortBy>();
            return new BsonDocumentWrapper(sortByBuilder, serializer);
        }

        /// <summary>
        /// The direction of the sort.
        /// </summary>
        public enum Direction
        {
            /// <summary>
            /// Ascending.
            /// </summary>
            Ascending,
            /// <summary>
            /// Descending.
            /// </summary>
            Descending
        }
    }

    /// <summary>
    /// A <see cref="String" /> based sort.
    /// </summary>
    /// <typeparam name="TDocument">The type of the document.</typeparam>
    public sealed class JsonSort<TDocument> : Sort<TDocument>
    {
        private readonly string _json;

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonSort{TDocument}"/> class.
        /// </summary>
        /// <param name="json">The json.</param>
        public JsonSort(string json)
        {
            _json = Ensure.IsNotNull(json, "json");
        }

        /// <inheritdoc />
        public override BsonDocument Render(IBsonSerializer<TDocument> documentSerializer, IBsonSerializerRegistry serializerRegistry)
        {
            return BsonDocument.Parse(_json);
        }
    }

    /// <summary>
    /// A <see cref="Object" /> based sort.
    /// </summary>
    /// <typeparam name="TDocument">The type of the document.</typeparam>
    public sealed class ObjectSort<TDocument> : Sort<TDocument>
    {
        private readonly object _obj;

        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectSort{TDocument}"/> class.
        /// </summary>
        /// <param name="obj">The object.</param>
        public ObjectSort(object obj)
        {
            _obj = Ensure.IsNotNull(obj, "obj");
        }

        /// <summary>
        /// Gets the object.
        /// </summary>
        public object Object
        {
            get { return _obj; }
        }

        /// <inheritdoc />
        public override BsonDocument Render(IBsonSerializer<TDocument> documentSerializer, IBsonSerializerRegistry serializerRegistry)
        {
            var serializer = serializerRegistry.GetSerializer(_obj.GetType());
            return new BsonDocumentWrapper(_obj, serializer);
        }
    }
}
