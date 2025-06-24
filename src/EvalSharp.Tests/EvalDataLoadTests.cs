namespace EvalSharp.Tests;

public class EvalDataLoadTests
{
    
    [Fact]
    public void FromCsvFileTest()
    {
        string relativePath = Path.Combine("TestData", "customer_shopping_data.csv");
        string fullPath = Path.Combine(AppContext.BaseDirectory, relativePath);
        var data = EvalData.FromCsvFile<CustomerShoppingRecord>(fullPath);

        Assert.NotNull(data);
        Assert.True(ValidateInnerFields(data));
        Assert.True(data.Count() == 100);
    }

    [Fact]
    public void FromCsvTest()
    {
        string relativePath = Path.Combine("TestData", "customer_shopping_data.csv");
        string fullPath = Path.Combine(AppContext.BaseDirectory, relativePath);
        var csvLines = File.ReadAllText(fullPath);
        var data = EvalData.FromCsv<CustomerShoppingRecord>(csvLines);

        Assert.NotNull(data);
        Assert.True(ValidateInnerFields(data));
        Assert.True(data.Count() == 100);
    }

    [Fact]
    public void FromJsonFileTest()
    {
        string relativePath = Path.Combine("TestData", "customer_shopping_data.json");
        string fullPath = Path.Combine(AppContext.BaseDirectory, relativePath);
        var data = EvalData.FromJsonFile<CustomerShoppingRecord>(fullPath);

        Assert.NotNull(data);
        Assert.True(ValidateInnerFields(data));
        Assert.True(data.Count() == 100);
    }


    [Fact]
    public async Task FromJsonFileAsyncTest()
    {
        string relativePath = Path.Combine("TestData", "customer_shopping_data.json");
        string fullPath = Path.Combine(AppContext.BaseDirectory, relativePath);
        var data = EvalData.FromJsonFileAsync<CustomerShoppingRecord>(fullPath);

        Assert.NotNull(data);
        int count = 0;
        await foreach(var item in data)
        {
            Assert.NotNull(item);
            if (item.CustomerID == Guid.Parse("4c487fb0-95fa-4d3d-99b7-fc884f41e992"))
            {
                Assert.True(ExampleCheckPassed(item));
            }
            count++;
        }
        Assert.True(count == 100);
    }

    [Fact]
    public void FromJsonTest()
    {
        string relativePath = Path.Combine("TestData", "customer_shopping_data.json");
        string fullPath = Path.Combine(AppContext.BaseDirectory, relativePath);
        var entireJson = File.ReadAllText(fullPath);
        var data = EvalData.FromJson<CustomerShoppingRecord>(entireJson);

        Assert.NotNull(data);
        Assert.True(ValidateInnerFields(data));
        Assert.True(data.Count() == 100);
    }

    [Fact]
    public void FromJsonLineTest()
    {
        var jsonL = $$"""{ "CustomerID":"4c487fb0-95fa-4d3d-99b7-fc884f41e992","Name":"Julia Coleman","Email":"lindahall@hotmail.com","ProductCategory":"Home & Garden","AmountSpent":328.31,"DateOfPurchase":"2025-05-20T00:00:00.000","PaymentMethod":"Gift Card","IsFirstPurchase":true}""";
        var data = EvalData.FromJsonL<CustomerShoppingRecord>(jsonL);

        Assert.NotNull(data);
        Assert.True(ValidateInnerFields(data));
        Assert.True(data.Count() == 1);
    }

    [Fact]
    public void FromJsonLinesTest()
    {
        var jsonL1 = $$"""{ "CustomerID":"4c487fb0-95fa-4d3d-99b7-fc884f41e992","Name":"Julia Coleman","Email":"lindahall@hotmail.com","ProductCategory":"Home & Garden","AmountSpent":328.31,"DateOfPurchase":"2025-05-20T00:00:00.000","PaymentMethod":"Gift Card","IsFirstPurchase":true}""";
        var jsonL2 = $$"""{ "CustomerID":"2d6de85d-5871-4a6c-9bbd-ec442164f790","Name":"Carla Logan","Email":"kylecaldwell@merritt.com","ProductCategory":"Sports","AmountSpent":180.15,"DateOfPurchase":"2024-07-28T00:00:00.000","PaymentMethod":"Debit Card","IsFirstPurchase":true}""";
        var jsonL3 = $$"""{ "CustomerID":"24d7d7e8-bbf1-4737-b223-0023720ec914","Name":"Oscar Garcia","Email":"paula22@gmail.com","ProductCategory":"Books","AmountSpent":491.98,"DateOfPurchase":"2025-05-02T00:00:00.000","PaymentMethod":"Gift Card","IsFirstPurchase":true}""";
        var jsonL4 = $$"""{ "CustomerID":"53ddc14f-30bb-4937-8b5d-44130ae332b4","Name":"Samantha Chandler","Email":"bdavis@wong-thornton.com","ProductCategory":"Groceries","AmountSpent":36.68,"DateOfPurchase":"2024-10-18T00:00:00.000","PaymentMethod":"Debit Card","IsFirstPurchase":true}""";
        var jsonL5 = $$"""{ "CustomerID":"4e52fbd6-d938-4f1f-9995-00ccfa32a8ce","Name":"Timothy Sexton","Email":"skirby@schmitt.com","ProductCategory":"Groceries","AmountSpent":397.85,"DateOfPurchase":"2025-05-21T00:00:00.000","PaymentMethod":"Credit Card","IsFirstPurchase":false}""";
        var jsonL6 = $$"""{ "CustomerID":"e66fc47e-5d66-41a8-a644-65a744ec3544","Name":"Lisa Wolf","Email":"kconley@yahoo.com","ProductCategory":"Clothing","AmountSpent":289.51,"DateOfPurchase":"2024-12-04T00:00:00.000","PaymentMethod":"PayPal","IsFirstPurchase":true}""";
        var jsonL7 = $$"""{ "CustomerID":"2f1d22ad-a29d-44a0-9a48-4a724319259c","Name":"David Buchanan","Email":"vanessarobinson@yahoo.com","ProductCategory":"Groceries","AmountSpent":71.92,"DateOfPurchase":"2024-10-31T00:00:00.000","PaymentMethod":"Gift Card","IsFirstPurchase":true}""";
        var jsonL8 = $$"""{ "CustomerID":"5c444b87-adea-44f4-b11c-907afef89e18","Name":"Travis Ramirez","Email":"latashajensen@trevino-alexander.net","ProductCategory":"Electronics","AmountSpent":369.57,"DateOfPurchase":"2024-10-08T00:00:00.000","PaymentMethod":"Debit Card","IsFirstPurchase":false}""";
        var jsonL9 = $$"""{ "CustomerID":"9375c2ad-d293-474a-8310-fd920c716be6","Name":"Jennifer Spence","Email":"michael73@harris.com","ProductCategory":"Groceries","AmountSpent":245.42,"DateOfPurchase":"2024-09-29T00:00:00.000","PaymentMethod":"Gift Card","IsFirstPurchase":true}""";
        var jsonL = new List<string> { jsonL1, jsonL2, jsonL3, jsonL4, jsonL5, jsonL6, jsonL7, jsonL8, jsonL9 };

        var data = EvalData.FromJsonL<CustomerShoppingRecord>(jsonL);

        Assert.NotNull(data);
        Assert.True(ValidateInnerFields(data));
        Assert.True(data.Count() == 9);
    }

    private static bool ValidateInnerFields(IEnumerable<CustomerShoppingRecord> records)
    {
        var itemToCheck = records.FirstOrDefault(r => r.CustomerID == Guid.Parse("4c487fb0-95fa-4d3d-99b7-fc884f41e992"));
        if (itemToCheck == null) return false;
        return ExampleCheckPassed(itemToCheck);
    }

    private static bool ExampleCheckPassed(CustomerShoppingRecord itemToCheck)
    {
        if (itemToCheck.Name != "Julia Coleman") return false;
        if (itemToCheck.Email != "lindahall@hotmail.com") return false;
        if (itemToCheck.ProductCategory != "Home & Garden") return false;
        if (itemToCheck.AmountSpent != 328.31m) return false;
        if (itemToCheck.DateOfPurchase != DateTime.Parse("2025-05-20T00:00:00.000")) return false;
        if (itemToCheck.PaymentMethod != "Gift Card") return false;
        if (!itemToCheck.IsFirstPurchase) return false;
        return true;
    }
}

public class CustomerShoppingRecord
{
    public Guid CustomerID { get; set; }          
    public string? Name { get; set; }             
    public string? Email { get; set; }            
    public string? ProductCategory { get; set; }  
    public decimal AmountSpent { get; set; }      
    public DateTime DateOfPurchase { get; set; }  
    public string? PaymentMethod { get; set; }    
    public bool IsFirstPurchase { get; set; }     
}