//
// LanguageNodeFormatter.cs: 
//   Common NodeInfo formatting code for Language-specific output
//
// Author: Jonathan Pryor (jonpryor@vt.edu)
//
// (C) 2002 Jonathan Pryor
//

using System;
using System.Collections;
using System.IO;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace Mono.TypeReflector
{
	public abstract class LanguageNodeFormatter : NodeFormatter {

		protected abstract string LineComment {get;}

		// [0] is open, [1] is close
		protected abstract string[] AttributeDelimeters {get;}

		protected abstract string KeywordClass {get;}
		protected abstract string KeywordEnum {get;}
		protected abstract string KeywordValueType {get;}
		protected abstract string KeywordInterface {get;}
		protected abstract string KeywordInherits {get;}
		protected abstract string KeywordImplements {get;}
		protected abstract string KeywordMulticast {get;}
		protected abstract string KeywordStatementTerminator {get;}
		protected abstract string KeywordStatementSeparator {get;}

		protected abstract string QualifierPublic {get;}
		protected abstract string QualifierFamily {get;}
		protected abstract string QualifierAssembly {get;}
		protected abstract string QualifierPrivate {get;}
		protected abstract string QualifierStatic {get;}
		protected abstract string QualifierFinal {get;}
		protected abstract string QualifierAbstract {get;}
		protected abstract string QualifierVirtual {get;}
		protected abstract string QualifierLiteral {get;}

		protected override string GetTypeDescription (Type type, object instance)
		{
			StringBuilder sb = new StringBuilder ();
			AddAttributes (sb, type);
			AddTypeQualifiers (sb, type);
			sb.AppendFormat ("{0} {1}", GetTypeKeyword(type), type.FullName);
			return sb.ToString ();
		}

		protected void AddAttribute (StringBuilder sb, string attribute)
		{
			sb.AppendFormat ("{0}{1}{2}", 
						AttributeDelimeters[0], attribute, AttributeDelimeters[1]);
		}

		protected void AddAttributes (StringBuilder sb, MemberInfo m)
		{
			AddAttributes (sb, m, true);
		}

		protected void AddAttributes (StringBuilder sb, MemberInfo m, bool newline)
		{
			AddCilAttributes (sb, m, newline);
			AddCustomAttributes (sb, m, "", newline);
		}

		private void AddCilAttributes (StringBuilder sb, MemberInfo m, bool newline)
		{
			Type t = m as Type;
			MethodBase mb = m as MethodBase;
			if (t != null)
				AddCilAttributes (sb, t, newline);
			else if (mb != null)
				AddCilAttributes (sb, mb, newline);
		}

		private void AddCilAttributes (StringBuilder sb, Type t, bool newline)
		{
			if (t.IsSerializable) {
				AddAttribute (sb, "Serializable");
				if (newline)
					sb.Append ("\n");
			}
		}

		private void AddCilAttributes (StringBuilder sb, MethodBase m, bool newline)
		{
			MethodImplAttributes attr = m.GetMethodImplementationFlags ();
			if ((attr & MethodImplAttributes.InternalCall) != 0) {
				AddAttribute (sb, "MethodImplAttribute(MethodImplOptions.InternalCall)");
				if (newline)
					sb.Append ("\n");
			}
		}

		protected void AddTypeQualifiers (StringBuilder sb, Type type)
		{
			if (type.IsPublic)
				sb.Append (QualifierPublic + " ");
			if (type.IsSealed && !type.IsValueType)
				sb.Append (QualifierFinal + " ");
			if (type.IsAbstract && !type.IsInterface)
				sb.Append (QualifierAbstract + " ");
		}

		new protected string GetTypeKeyword (Type type)
		{
			if (type.IsClass)
				return KeywordClass;
			if (type.IsEnum)
				return KeywordEnum;
			if (type.IsValueType)
				return KeywordValueType;
			if (type.IsInterface)
				return KeywordInterface;

			// unknown type
			return "type";
		}

		protected override string GetBaseTypeDescription (Type type, object instance)
		{
			if (type != null)
				return string.Format ("{0} {1}", KeywordInherits, type.Name);
			return string.Format ("{0} No Base Type", LineComment);
		}

		protected override string GetInterfaceDescription (Type type, object instance)
		{
			return string.Format ("{0} {1}", KeywordImplements, type.Name);
		}

		protected override string GetConstructorDescription (ConstructorInfo ctor, object instance)
		{
			StringBuilder sb = new StringBuilder ();
			AddAttributes (sb, ctor);
			AddMethodQualifiers (sb, ctor);
			sb.AppendFormat ("{0} ", GetConstructorName (ctor));
			AddMethodArgs (sb, ctor);

			return sb.ToString();
		}

		protected void AddMethodQualifiers (StringBuilder sb, MethodBase m)
		{
			if (m.IsPublic)
				sb.AppendFormat ("{0} ", QualifierPublic);
			if (m.IsFamily)
				sb.AppendFormat ("{0} ", QualifierFamily);
			if (m.IsAssembly)
				sb.AppendFormat ("{0} ", QualifierAssembly);
			if (m.IsPrivate)
				sb.AppendFormat ("{0} ", QualifierPrivate);
			if (m.IsStatic)
				sb.AppendFormat ("{0} ", QualifierStatic);
			if (m.IsFinal)
				sb.AppendFormat ("{0} ", QualifierFinal);
			if (m.IsAbstract)
				sb.AppendFormat ("{0} ", QualifierAbstract);
			else if (m.IsVirtual)
				sb.AppendFormat ("{0} ", QualifierVirtual);
		}

		protected abstract string GetConstructorName (ConstructorInfo ctor);

		protected void AddMethodArgs (StringBuilder sb, MethodBase m)
		{
			sb.Append ("(");
			ParameterInfo[] parms = m.GetParameters ();
			if (parms.Length != 0) {
				int cur = 0;
				foreach (ParameterInfo pi in parms) {
					sb.Append (GetParameterDescription (pi, pi));
					if (cur++ != (parms.Length-1))
						sb.Append (", ");
				}
			}
			sb.Append (")");
		}

		protected override string GetEventDescription (EventInfo e, object instance)
		{
			StringBuilder sb = new StringBuilder ();
			AddMethodQualifiers (sb, e.GetAddMethod(true));
			return string.Format ("{0}{1}{2} {3}",
					sb.ToString (),
					e.IsMulticast ? KeywordMulticast + " " : "",
					e.EventHandlerType,
					e.Name);
		}

		protected override string GetFieldDescription (FieldInfo field, object instance)
		{
			StringBuilder sb = new StringBuilder ();
			AddAttributes (sb, field);

			if (!field.DeclaringType.IsEnum || field.IsSpecialName) {
				AddFieldQualifiers (sb, field);
				sb.AppendFormat ("{0} ", field.FieldType);
			}

			sb.Append (field.Name);

			try {
				sb.AppendFormat (" = {0}", GetValue (field.GetValue (instance)));
			}
			catch {
			}

			if (!field.DeclaringType.IsEnum || field.IsSpecialName)
				sb.Append (KeywordStatementTerminator);
			else
				sb.Append (KeywordStatementSeparator);

			return sb.ToString();
		}

		protected void AddFieldQualifiers (StringBuilder sb, FieldInfo field)
		{
			if (field.IsPublic)
				sb.AppendFormat ("{0} ", QualifierPublic);
			if (field.IsPrivate)
				sb.AppendFormat ("{0} ", QualifierPrivate);
			if (field.IsAssembly)
				sb.AppendFormat ("{0} ", QualifierAssembly);
			if (field.IsFamily)
				sb.AppendFormat ("{0} ", QualifierFamily);
			if (field.IsLiteral)
				sb.AppendFormat ("{0} ", QualifierLiteral);
			else if (field.IsStatic)
				sb.AppendFormat ("{0} ", QualifierStatic);
		}

		protected override string GetMethodDescription (MethodInfo method, object instance)
		{
			StringBuilder sb = new StringBuilder ();

			if (method.IsSpecialName)
				sb.AppendFormat ("{0} Method is a specially named method:\n", LineComment);

			AddAttributes (sb, method);
			if (method.ReturnTypeCustomAttributes != null)
				AddCustomAttributes (sb, method.ReturnTypeCustomAttributes, "return: ", true);

			AddMethodQualifiers (sb, method);

      AddMethodDeclaration (sb, method);

			if (method.GetParameters().Length == 0) {
				try {
					object r = method.Invoke (instance, null);
					string s = GetValue (r);
					sb.AppendFormat (" {0} = {1}", LineComment, s);
				}
				catch {
				}
			}

			string r = sb.ToString();

			if (method.IsSpecialName)
				return r.Replace ("\n", "\n" + LineComment + "\t");
			return r;
		}

		protected virtual void AddMethodDeclaration (StringBuilder sb, MethodInfo method)
		{
			sb.AppendFormat ("{0} {1} ", method.ReturnType, method.Name);

			AddMethodArgs (sb, method);
		}

		protected override string GetParameterDescription (ParameterInfo param, object instance)
		{
			StringBuilder sb = new StringBuilder ();

			AddCustomAttributes (sb, param, "", false);
			AddParameterAttributes (sb, param.Attributes);

			sb.AppendFormat ("{0} {1}", param.ParameterType, param.Name);

			return sb.ToString();
		}

		protected void AddParameterAttributes (StringBuilder sb, ParameterAttributes attrs)
		{
			if ((attrs & ParameterAttributes.In) != 0)
				sb.Append ("in");
			if ((attrs & ParameterAttributes.Out) != 0)
				sb.Append ("out");
			if ((attrs & ParameterAttributes.Lcid) != 0)
				sb.Append ("lcid");
			if ((attrs & ParameterAttributes.Retval) != 0)
				sb.Append ("retval");
			if ((attrs & ParameterAttributes.Optional) != 0)
				sb.Append ("optional");
			if ((attrs & ParameterAttributes.HasDefault) != 0)
				sb.Append ("hasdefault");
			if ((attrs & ParameterAttributes.HasFieldMarshal) != 0)
				sb.Append ("hasfieldmarshal");
			if ((attrs & ParameterAttributes.ReservedMask) != 0)
				sb.Append ("reservedmask");
			if ((attrs & ParameterAttributes.Reserved3) != 0)
				sb.Append ("reserved3");
			if ((attrs & ParameterAttributes.Reserved4) != 0)
				sb.Append ("reserved4");
		}

		protected override string GetPropertyDescription (PropertyInfo property, object instance)
		{
			StringBuilder sb = new StringBuilder ();
			AddAttributes (sb, property);
			AddMethodQualifiers (sb, property.GetAccessors(true)[0]);

			sb.AppendFormat ("{0} {1} {{", property.PropertyType, property.Name);
			if (property.CanRead) {
				sb.Append ("get");
				try {
					sb.AppendFormat (" /* = {0} */", GetValue (property.GetValue (instance, null)));
				}
				catch {
				}
				sb.Append (";");
			}
			if (property.CanWrite)
				sb.Append ("set;");
			sb.Append ("}}");

			return sb.ToString();
		}

		protected override string GetReturnValueDescription (NodeInfo n)
		{
			return string.Format ("{0} ReturnValue={1}", 
					LineComment, GetOtherDescription (n));
		}

		protected override string GetOtherDescription (NodeInfo node)
		{
			if (node.Description != null)
				return node.Description.ToString();
			return "<null other description/>";
		}

		protected void AddCustomAttributes (StringBuilder sb, ICustomAttributeProvider m, string attributeType, bool newLine)
		{
			object[] attrs = m.GetCustomAttributes (true);

			foreach (object a in attrs) {

				AddCustomAttribute (sb, a, attributeType);

				if (newLine)
					sb.Append ("\n");
			}
		}

		protected virtual void AddCustomAttribute (StringBuilder sb, object attribute, string attributeType)
		{
			Type type = attribute.GetType();
			sb.AppendFormat ("{0}{1}{2}", AttributeDelimeters[0], attributeType, type.FullName);

			PropertyInfo[] props = type.GetProperties();
			FieldInfo[] fields = type.GetFields();

			if (props.Length > 0 || fields.Length > 0) {
				sb.Append ("(");
				if (props.Length > 0) {
					AddPropertyValues (sb, props, attribute);
					if (fields.Length > 0)
						sb.Append (", ");
				}
				if (fields.Length > 0)
					AddFieldValues (sb, fields, attribute);
				sb.Append (")");
			}
			sb.Append (AttributeDelimeters[1]);
		}

		private void AddPropertyValues (StringBuilder sb, PropertyInfo[] props, object instance)
		{
			int len = props.Length;
			for (int i = 0; i != len; ++i) {
				sb.Append (props[i].Name);
				sb.Append ("=");
				try {
					sb.Append (GetEncodedValue (props[i].GetValue (instance, null)));
				} catch {
					sb.Append ("<exception/>");
				}
				if (i != (len-1))
					sb.Append (", ");
			}
		}

		private void AddFieldValues (StringBuilder sb, FieldInfo[] fields, object instance)
		{
			int len = fields.Length;
			for (int i = 0; i != len; ++i) {
				sb.Append (fields[i].Name);
				sb.Append ("=");
				sb.Append (GetEncodedValue (fields[i].GetValue (instance)));
				if (i != (len-1))
					sb.Append (", ");
			}
		}

		protected virtual string GetEncodedValue (object value)
		{
			if (value == null)
				return "null";

			switch (Type.GetTypeCode(value.GetType())) {
				case TypeCode.Char:
					return GetEncodedCharValue (value);
				case TypeCode.Decimal:
					return GetEncodedDecimalValue (value);
				case TypeCode.Double:
					return GetEncodedDoubleValue (value);
				case TypeCode.Int64:
					return GetEncodedInt64Value (value);
				case TypeCode.Single:
					return GetEncodedSingleValue (value);
				case TypeCode.String:
					return GetEncodedStringValue (value);
				case TypeCode.UInt32:
					return GetEncodedUInt32Value (value);
				case TypeCode.UInt64:
					return GetEncodedUInt64Value (value);
				case TypeCode.Object:
					return GetEncodedObjectValue (value);
			}
			// not special-cased; just return it's value
			return value.ToString();
		}

		protected virtual string GetEncodedCharValue (object value)
		{
			return String.Format ("'{0}'", value.ToString());
		}

		protected virtual string GetEncodedDecimalValue (object value)
		{
			return String.Format ("{0}m", value.ToString());
		}

		protected virtual string GetEncodedDoubleValue (object value)
		{
			return String.Format ("{0}d", value.ToString());
		}

		protected virtual string GetEncodedInt64Value (object value)
		{
			return String.Format ("{0}L", value.ToString());
		}

		protected virtual string GetEncodedSingleValue (object value)
		{
			return String.Format ("{0}f", value.ToString());
		}

		protected virtual string GetEncodedStringValue (object value)
		{
			return String.Format ("\"{0}\"", value.ToString());
		}

		protected virtual string GetEncodedUInt32Value (object value)
		{
			return String.Format ("{0}U", value.ToString());
		}

		protected virtual string GetEncodedUInt64Value (object value)
		{
			return String.Format ("{0}UL", value.ToString());
		}

		protected virtual string GetEncodedObjectValue (object value)
		{
			return String.Format ("typeof({0})", value.ToString());
		}
	}
}

