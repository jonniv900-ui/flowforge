Imports System
Imports System.Collections.Generic
Imports System.ComponentModel
Imports System.Linq

Namespace FlowForgeStudio
    Friend NotInheritable Class DesignObjectAdapter
        Implements ICustomTypeDescriptor
        Private ReadOnly target As Object

        Public Sub New(value As Object)
            target = value
        End Sub
        Public Function GetAttributes() As AttributeCollection Implements ICustomTypeDescriptor.GetAttributes
            Return TypeDescriptor.GetAttributes(target, True)
        End Function
        Public Function GetClassName() As String Implements ICustomTypeDescriptor.GetClassName
            Return TypeDescriptor.GetClassName(target, True)
        End Function
        Public Function GetComponentName() As String Implements ICustomTypeDescriptor.GetComponentName
            Return TypeDescriptor.GetComponentName(target, True)
        End Function
        Public Function GetConverter() As TypeConverter Implements ICustomTypeDescriptor.GetConverter
            Return TypeDescriptor.GetConverter(target, True)
        End Function
        Public Function GetDefaultEvent() As EventDescriptor Implements ICustomTypeDescriptor.GetDefaultEvent
            Return TypeDescriptor.GetDefaultEvent(target, True)
        End Function
        Public Function GetDefaultProperty() As PropertyDescriptor Implements ICustomTypeDescriptor.GetDefaultProperty
            Return TypeDescriptor.GetDefaultProperty(target, True)
        End Function
        Public Function GetEditor(editorBaseType As Type) As Object Implements ICustomTypeDescriptor.GetEditor
            Return TypeDescriptor.GetEditor(target, editorBaseType, True)
        End Function
        Public Function GetEvents() As EventDescriptorCollection Implements ICustomTypeDescriptor.GetEvents
            Return TypeDescriptor.GetEvents(target, True)
        End Function
        Public Function GetEvents(attributes As Attribute()) As EventDescriptorCollection Implements ICustomTypeDescriptor.GetEvents
            Return TypeDescriptor.GetEvents(target, attributes, True)
        End Function
        Public Function GetProperties() As PropertyDescriptorCollection Implements ICustomTypeDescriptor.GetProperties
            Return BuildProperties(Nothing)
        End Function
        Public Function GetProperties(attributes As Attribute()) As PropertyDescriptorCollection Implements ICustomTypeDescriptor.GetProperties
            Return BuildProperties(attributes)
        End Function
        Public Function GetPropertyOwner(pd As PropertyDescriptor) As Object Implements ICustomTypeDescriptor.GetPropertyOwner
            Return target
        End Function
        Private Function BuildProperties(filter As Attribute()) As PropertyDescriptorCollection
            ' Preserve a coleção filtrada original do PropertyGrid. Consulte a coleção
            ' completa apenas para recuperar Name quando o WinForms a marca como oculta.
            Dim original As PropertyDescriptorCollection = If(filter Is Nothing,
                TypeDescriptor.GetProperties(target, True),
                TypeDescriptor.GetProperties(target, filter, True))
            Dim result As New List(Of PropertyDescriptor)()
            For Each descriptor As PropertyDescriptor In original
                If descriptor.Name.Equals("Name", StringComparison.OrdinalIgnoreCase) Then
                    result.Add(New VisibleNamePropertyDescriptor(descriptor))
                Else
                    result.Add(descriptor)
                End If
            Next
            If Not result.Any(Function(p) p.Name.Equals("Name", StringComparison.OrdinalIgnoreCase)) Then
                Dim hiddenName As PropertyDescriptor = TypeDescriptor.GetProperties(target, True)("Name")
                If hiddenName IsNot Nothing Then
                    Dim visibleName As New VisibleNamePropertyDescriptor(hiddenName)
                    If filter Is Nothing OrElse filter.Length = 0 OrElse visibleName.Attributes.Matches(filter) Then
                        result.Insert(0, visibleName)
                    End If
                End If
            End If
            Return New PropertyDescriptorCollection(result.ToArray(), True)
        End Function
    End Class

    Friend NotInheritable Class VisibleNamePropertyDescriptor
        Inherits PropertyDescriptor
        Private ReadOnly original As PropertyDescriptor

        Public Sub New(value As PropertyDescriptor)
            MyBase.New(value.Name, VisibleAttributes(value))
            original = value
        End Sub
        Private Shared Function VisibleAttributes(value As PropertyDescriptor) As Attribute()
            Dim list = value.Attributes.Cast(Of Attribute)().Where(Function(a) Not TypeOf a Is BrowsableAttribute).ToList()
            list.Add(BrowsableAttribute.Yes)
            list.Add(New CategoryAttribute("Design"))
            Return list.ToArray()
        End Function
        Public Overrides ReadOnly Property ComponentType As Type
            Get
                Return original.ComponentType
            End Get
        End Property
        Public Overrides ReadOnly Property IsReadOnly As Boolean
            Get
                Return original.IsReadOnly
            End Get
        End Property
        Public Overrides ReadOnly Property PropertyType As Type
            Get
                Return original.PropertyType
            End Get
        End Property
        Public Overrides Function CanResetValue(component As Object) As Boolean
            Return original.CanResetValue(component)
        End Function
        Public Overrides Function GetValue(component As Object) As Object
            Return original.GetValue(component)
        End Function
        Public Overrides Sub ResetValue(component As Object)
            original.ResetValue(component)
        End Sub
        Public Overrides Sub SetValue(component As Object, value As Object)
            original.SetValue(component, value)
        End Sub
        Public Overrides Function ShouldSerializeValue(component As Object) As Boolean
            Return original.ShouldSerializeValue(component)
        End Function
    End Class
End Namespace
