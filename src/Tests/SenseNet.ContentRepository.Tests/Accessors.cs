﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Storage.Schema;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using SenseNet.Testing;
using SenseNet.Tests.Core;

namespace SenseNet.ContentRepository.Tests
{
    internal class SchemaEditorAccessor : Accessor
    {
        public SchemaEditorAccessor(SchemaEditor target) : base(target) { }
        public void RegisterSchema(SchemaEditor origSchema, SchemaWriter writer)
        {
            //private void RegisterSchema(SchemaEditor origSchema, SchemaEditor newSchema, SchemaWriter schemaWriter)
            CallPrivateStaticMethod("RegisterSchema", new Type[] { typeof(SchemaEditor), typeof(SchemaEditor), typeof(SchemaWriter) }, origSchema, _target, writer);
        }
    }
    internal class SchemaItemAccessor : Accessor
    {
        public SchemaItemAccessor(SchemaItem target) : base(target) { }
        public int Id
        {
            get { return ((SchemaItem)_target).Id; }
            set { SetPrivateField("_id", value); }
        }
    }

    internal class ContentTypeManagerAccessor : Accessor
    {
        public ContentTypeManagerAccessor(ContentTypeManager target) : base(target) { }
        public ContentType LoadOrCreateNew(string contentTypeDefinitionXml)
        {
            return (ContentType)CallPrivateStaticMethod("LoadOrCreateNew", new Type[] { typeof(string) }, contentTypeDefinitionXml);
        }
        public void ApplyChangesInEditor(ContentType settings, SchemaEditor editor)
        {
            base.CallPrivateStaticMethod("ApplyChangesInEditor", new Type[] { typeof(ContentType), typeof(SchemaEditor) }, settings, editor);
        }
        public NodeTypeRegistrationAccessor ParseAttributes(Type type)
        {
            object ntr = base.CallPrivateStaticMethod("ParseAttributes", new Type[] { typeof(Type) }, type);
            if (ntr == null)
                return null;
            NodeTypeRegistrationAccessor ntrAcc = new NodeTypeRegistrationAccessor(ntr);
            return ntrAcc;
        }
    }
    internal class NodeTypeRegistrationAccessor : Accessor
    {
        private List<PropertyTypeRegistrationAccessor> _propertyTypeRegistrations;

        public Type Type
        {
            get { return (Type)GetPublicValue("Type"); }
            set { SetPublicValue("Type", value); }
        }
        public string Name
        {
            get { return (string)GetPublicValue("Name"); }
        }
        public string ParentName
        {
            get { return (string)GetPublicValue("ParentName"); }
            set { SetPublicValue("ParentName", value); }
        }
        public List<PropertyTypeRegistrationAccessor> PropertyTypeRegistrations
        {
            get { return _propertyTypeRegistrations; }
        }

        public NodeTypeRegistrationAccessor(object target)
            : base(target)
        {
            _propertyTypeRegistrations = new List<PropertyTypeRegistrationAccessor>();
            object listobject = GetPublicValue("PropertyTypeRegistrations");
            IEnumerable list = listobject as IEnumerable;
            foreach (object item in list)
            {
                PropertyTypeRegistrationAccessor p = new PropertyTypeRegistrationAccessor(item);
                p.Parent = this;
                _propertyTypeRegistrations.Add(p);
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("NTR: name="); sb.Append(this.Name).Append(", ParentName=").Append(this.ParentName ?? "[null]");
            sb.Append(", Type=").Append(this.Type.FullName ?? "[null]").Append("\r\n");
            foreach (PropertyTypeRegistrationAccessor ptrAcc in _propertyTypeRegistrations)
                sb.Append("\t").Append(ptrAcc).Append("\r\n");
            return sb.ToString();
        }
    }
    internal class PropertyTypeRegistrationAccessor : Accessor
    {
        private NodeTypeRegistrationAccessor _parent;

        public string Name
        {
            get { return (string)GetPublicValue("Name"); }
        }
        public RepositoryDataType DataType
        {
            get { return (RepositoryDataType)GetPublicValue("DataType"); }
            set { SetPublicValue("DataType", value); }
        }
        public bool IsDeclared
        {
            get { return (bool)GetPublicValue("IsDeclared"); }
            set { SetPublicValue("IsDeclared", value); }
        }
        public NodeTypeRegistrationAccessor Parent
        {
            get { return _parent; }
            set { _parent = value; }
        }

        public PropertyTypeRegistrationAccessor(object target) : base(target) { }

        public override string ToString()
        {
            return String.Concat("PTR: name=", this.Name, ", DataType=", this.DataType, ", IsDeclared=", this.IsDeclared);
        }
    }

    internal class TypeCollectionAccessor<T> : Accessor, IEnumerable<T> where T : SchemaItem
    {
        public TypeCollection<T> Target { get { return (TypeCollection<T>)_target; } }

        public static TypeCollectionAccessor<T> Create(SchemaEditor schemaEditor)
        {
            var prt = new TypeAccessor("SenseNet.Storage", "SenseNet.ContentRepository.Storage.Schema.ISchemaRoot");
            Type argType = prt.TargetType;
            var pro = new ObjectAccessor(typeof(TypeCollection<T>), new Type[] { argType }, new object[] { schemaEditor });
            return new TypeCollectionAccessor<T>(pro.Target);
        }
        public static TypeCollectionAccessor<T> Create(TypeCollection<T> initialList)
        {
            var pro = new ObjectAccessor(typeof(TypeCollection<T>), new Type[] { typeof(TypeCollection<T>) }, new object[] { initialList });
            return new TypeCollectionAccessor<T>(pro.Target);
        }

        public TypeCollectionAccessor(object target) : base(target) { }

        public void Add(T item)
        {
            CallPrivateMethod("Add", item);
        }
        public int Count
        {
            get { return Target.Count; }
        }
        public T this[string name]
        {
            get { return Target[name]; }
            set
            {
                var indexerInfo = Target.GetType().GetMethod("set_Item", BindingFlags.Instance | BindingFlags.NonPublic, null, new Type[] { typeof(string), typeof(T) }, null);
                indexerInfo.Invoke(_target, new object[] { name, value });
            }
        }
        public T this[int index]
        {
            get { return Target[index]; }
            set
            {
                var indexerInfo = Target.GetType().GetMethod("set_Item", BindingFlags.Instance | BindingFlags.NonPublic, null, new Type[] { typeof(int), typeof(T) }, null);
                indexerInfo.Invoke(_target, new object[] { index, value });
            }
        }

        public bool Contains(T item)
        {
            return Target.Contains(item);
        }
        public int IndexOf(T item)
        {
            return Target.IndexOf(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            Target.CopyTo(array, arrayIndex);
        }
        public T[] CopyTo(int arrayIndex)
        {
            return Target.CopyTo(arrayIndex);
        }
        public T[] ToArray()
        {
            return Target.ToArray();
        }
        public int[] ToIdArray()
        {
            return Target.ToIdArray();
        }
        public string[] ToNameArray()
        {
            return Target.ToNameArray();
        }

        public IEnumerator<T> GetEnumerator()
        {
            return Target.GetEnumerator();
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return Target.GetEnumerator();
        }

        public T GetItemById(int itemId)
        {
            return Target.GetItemById(itemId);
        }

        public void AddRange(TypeCollection<T> items)
        {
            CallPrivateMethod("AddRange", items);
        }
        public void Insert(int index, T item)
        {
            CallPrivateMethod("Insert", index, item);
        }
        public void Clear()
        {
            CallPrivateMethod("Clear");
        }
        internal bool Remove(T item)
        {
            return (bool)CallPrivateMethod("Remove", item);
        }
        internal void RemoveAt(int index)
        {
            CallPrivateMethod("RemoveAt", index);
        }

    }

}
