﻿// Copyright (c) Argo Zhang (argo@163.com). All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
// Website: https://www.blazor.zone or https://argozhang.github.io/

using BootstrapBlazor.Shared;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Reflection;

namespace UnitTest.Localization;

public class UtilityTest : BootstrapBlazorTestBase
{
    private IStringLocalizer<Foo> Localizer { get; }

    public UtilityTest()
    {
        Localizer = Context.Services.GetRequiredService<IStringLocalizer<Foo>>();
    }

    [Fact]
    public void GetDisplayName_Ok()
    {
        var fooData = new DataTable();
        fooData.Columns.Add(new DataColumn(nameof(Foo.DateTime), typeof(DateTime)) { DefaultValue = DateTime.Now });
        fooData.Columns.Add(nameof(Foo.Name), typeof(string));
        fooData.Columns.Add(nameof(Foo.Complete), typeof(bool));
        fooData.Columns.Add(nameof(Foo.Education), typeof(string));
        fooData.Columns.Add(nameof(Foo.Count), typeof(int));
        Foo.GenerateFoo(Localizer, 10).ForEach(f =>
        {
            fooData.Rows.Add(f.DateTime, f.Name, f.Complete, f.Education, f.Count);
        });

        Assert.Equal("日期", Localizer[nameof(Foo.DateTime)]);

        var context = new DataTableDynamicContext(fooData, (context, col) =>
        {
            var propertyName = col.GetFieldName();
            if (propertyName == nameof(Foo.DateTime))
            {
                context.AddRequiredAttribute(nameof(Foo.DateTime));
                // 使用 AutoGenerateColumnAttribute 设置显示名称示例
                context.AddAutoGenerateColumnAttribute(nameof(Foo.DateTime), new KeyValuePair<string, object?>[] {
                        new(nameof(AutoGenerateColumnAttribute.Text), Localizer[nameof(Foo.DateTime)].Value)
                });
            }
            else if (propertyName == nameof(Foo.Name))
            {
                context.AddRequiredAttribute(nameof(Foo.Name), Localizer["Name.Required"].Value);
                // 使用 Text 设置显示名称示例
                col.Text = Localizer[nameof(Foo.Name)];
            }
            else if (propertyName == nameof(Foo.Count))
            {
                context.AddRequiredAttribute(nameof(Foo.Count));
                // 使用 DisplayNameAttribute 设置显示名称示例
                context.AddDisplayNameAttribute(nameof(Foo.Count), Localizer[nameof(Foo.Count)].Value);
            }
            else if (propertyName == nameof(Foo.Complete))
            {
                col.Filterable = true;
                // 使用 DisplayAttribute 设置显示名称示例
                context.AddDisplayAttribute(nameof(Foo.Complete), new KeyValuePair<string, object?>[] {
                        new(nameof(DisplayAttribute.Name), Localizer[nameof(Foo.Complete)].Value)
                });
                context.AddDescriptionAttribute(nameof(Foo.Complete), "Test-Desc");
            }
        });

        // 静态类
        var dn = Utility.GetDisplayName(typeof(Foo), nameof(Foo.Count));
        Assert.Equal("数量", dn);

        // 动态类
        dn = Utility.GetDisplayName(context.GetItems().First(), nameof(Foo.Count));
        Assert.Equal("数量", dn);

        // 静态类
        dn = Utility.GetDisplayName(typeof(Foo), nameof(Foo.Education));
        Assert.Equal("学历", dn);

        // 静态类
        dn = Utility.GetDisplayName(new Foo() { Education = EnumEducation.Middel }, nameof(Foo.Education));
        Assert.Equal("学历", dn);

        // 动态类
        dn = Utility.GetDisplayName(context.GetItems().First(), nameof(Foo.Education));
        Assert.Equal("Education", dn);

        var pi = context.GetItems().First().GetType().GetProperties().FirstOrDefault(i => i.CustomAttributes.Any(a => a.AttributeType == typeof(DescriptionAttribute)));
        var attr = pi?.GetCustomAttribute<DescriptionAttribute>();
        Assert.Equal("Test-Desc", attr?.Description);
    }

    [Fact]
    public void SetValue_Null()
    {
        var model = new MockDynamicObject();
        var context = new MockDynamicObjectContext();

        // 内部走 null 逻辑
        _ = context.SetValue(model);
    }

    private class MockDynamicObject : IDynamicObject
    {
        public Guid DynamicObjectPrimaryKey { get; set; }

        public object? GetValue(string propertyName)
        {
            throw new NotImplementedException();
        }

        public void SetValue(string propertyName, object? value)
        {
            throw new NotImplementedException();
        }
    }

    private class MockDynamicObjectContext : IDynamicObjectContext
    {
        public Func<IDynamicObject, ITableColumn, object?, Task>? OnValueChanged { get; set; }
        public Func<DynamicObjectContextArgs, Task>? OnChanged { get; set; }

        public Task AddAsync(IEnumerable<IDynamicObject> selectedItems)
        {
            throw new NotImplementedException();
        }

        public Task<bool> DeleteAsync(IEnumerable<IDynamicObject> items)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<ITableColumn> GetColumns()
        {
            throw new NotImplementedException();
        }

        public IEnumerable<IDynamicObject> GetItems() => new MockDynamicObject[] { new() { DynamicObjectPrimaryKey = Guid.NewGuid() } };
    }

    [Fact]
    public void GetPropertyValue_Ok()
    {
        var foo = Foo.Generate(Localizer);

        var v1 = Utility.GetPropertyValue<Foo, string>(foo, nameof(Foo.Name));
        Assert.Contains("张三", v1);

        var v2 = Utility.GetPropertyValue<object, object>(foo, nameof(Foo.Name));
        Assert.Contains("张三", v2.ToString());

        var v3 = Utility.GetPropertyValue(foo, nameof(Foo.Name));
        Assert.NotNull(v3);
        Assert.Contains("张三", v3!.ToString());
    }

    [Fact]
    public void GetPropertyValue_Null()
    {
        Foo? foo = null;
        Assert.Throws<ArgumentNullException>(() => Utility.GetPropertyValue<object?, string>(foo, nameof(Foo.Name)));
    }

    [Fact]
    public void SetPropertyValue_Ok()
    {
        var foo = Foo.Generate(Localizer);
        var v1 = "张三";
        var val = "李四";
        Utility.SetPropertyValue<Foo, string>(foo, nameof(Foo.Name), val);
        Assert.Equal(foo.Name, val);

        foo.Name = v1;
        Utility.SetPropertyValue<Foo, object>(foo, nameof(Foo.Name), val);
        Assert.Equal(foo.Name, val);

        foo.Name = v1;
        Utility.SetPropertyValue<object, string>(foo, nameof(Foo.Name), val);
        Assert.Equal(foo.Name, val);

        foo.Name = v1;
        Utility.SetPropertyValue<object, object>(foo, nameof(Foo.Name), val);
        Assert.Equal(foo.Name, val);
    }

    [Fact]
    public void SetPropertyValue_Null()
    {
        Foo? foo = null;
        Assert.Throws<ArgumentNullException>(() => Utility.SetPropertyValue<object?, object>(foo, nameof(Foo.Name), "1"));
    }

    [Fact]
    public void TryGetProperty_Ok()
    {
        var condition = Utility.TryGetProperty(typeof(Foo), nameof(Foo.Name), out _);
        Assert.True(condition);

        condition = Utility.TryGetProperty(typeof(Foo), "Test1", out _);
        Assert.False(condition);
    }
}
