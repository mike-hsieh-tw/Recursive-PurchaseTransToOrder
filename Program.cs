// See https://aka.ms/new-console-template for more information
using AutoMapper;

Console.WriteLine("Hello, World!");

Main();

/// <summary>
/// 功能說明: 將採購單依照特定條件轉結為訂購單。
///		條件一:	每張訂購單的總金額不能大於 25,000,000
///		條件二:	未轉訂購單的資料才要轉訂購單
///		條件三:	報價單種類為 2-特殊報價單，需要進行分類
///		條件四:	依照 [運送方式 ShipId], [報價單種類 QuotationType], [類別 Reg_Yn], [特報單類別 QuotationNo] 分類
///		條件五:	特定機型有對應的特定數量，該訂單的數量不能超過該特定數量
///		
/// </summary>
static void Main()
{
	//	取得資料
	var inputList = GetInputList();

	List<List<InputClass>> totalList = new List<List<InputClass>>();

	//	Step1. 篩選 Trans_Order_Yn = "N"， 未轉訂單的資料
	//  Step2. 只有 2: 特殊報價單要分類，所以其他都轉 null
	//	Step3. 依照 [運送方式 Ship_Id], [報價單種類 QUOTE_TYPE], [類別 Reg_Yn], [特報單類別 QUOTE_NO] 分類
	var groupData = inputList.Where(p => p.TransOrderYn == "N").Select(p =>
	{
		p.QuotationNo = p.QuotationType == "2" ? p.QuotationNo : null;   //  1: 一般報價單 就不分類，只有 2: 特殊報價單要分類
		return p;
	}).GroupBy(p => new { p.ShipId, p.QuotationType, p.TransOrderYn, p.QuotationNo });

	foreach (var item in groupData) {
		//	遞迴取得資料，並且加總起來
		totalList.AddRange(Recursive(item.ToList()));
	};
}

static List<List<InputClass>> Recursive(List<InputClass> items)
{
	//	初始化 AutoMapper
	var mapper = new MapperConfiguration(cfg =>
	{
		cfg.CreateMap<InputClass, InputClass>();
	}).CreateMapper(); ;

	List<List<InputClass>> outter = new List<List<InputClass>>();

	List<InputClass> inner = new List<InputClass>();

	decimal? totalPrice = 0;

	//  篩選轉置未結束的採購單
	foreach (var t in items.Where(p => p.Done == false && p.TransOrderYn == "N"))
	{
		if ((t.Price * t.Qty) + totalPrice <= 25_000_000)
		{
			//  檢查特定機型的數量是否大於貨櫃上限數量
			var CheckSpecificPartNumResult = CheckSpecificPartNum(t.ProdNo, t.Qty);

			//  若是特定機型大於貨櫃上限數量
			if (CheckSpecificPartNumResult.Item1)
			{
				//  <複製採購單>
				var target = new InputClass();
				mapper.Map(t, target);

				target.Qty = CheckSpecificPartNumResult.Item2;  // 修改<複製採購單>數量為上限數量

				outter.Add(new List<InputClass>() { target });  //	將<複製採購單>加到最外部的集合

				t.Qty -= CheckSpecificPartNumResult.Item2; // 現有採購單減掉限制數量

				var rtnData = Recursive(items); //  繼續遞迴

				if (rtnData.Count > 0) outter.AddRange(rtnData);    //  加入回傳集合中
			}
			//  特定機型未大於貨櫃上限數量
			else
			{
				totalPrice += (t.Price * t.Qty);  //  金額加入當前的訂單上限總金額

				inner.Add(t);  //  加入回傳的集合中

				t.Done = true;  //  該筆 採購單 轉 訂單 結束
			}
		}
		else
		{
			int canUseQty = Convert.ToInt32(Math.Floor(((25_000_000 - totalPrice) / t.Price) ?? 0)); //	取得不超過限制金額的數量

			// 複製一筆
			var target = new InputClass();
			mapper.Map(t, target);

			target.Qty = canUseQty; // 數量修正為不超過限制金額的數量

			inner.Add(target);  //  <複製採購單>加入集合

			t.Qty -= canUseQty; // 減掉限制數量

			// 繼續往下走
			var rtnData = Recursive(items);

			if (rtnData.Count > 0) outter.AddRange(rtnData);
		}
	}

	if(inner.Count > 0) outter.Add(inner);

	return outter;
}

static List<InputClass> GetInputList()
{
	var inputList = new List<InputClass>();
	inputList.Add(new InputClass()
	{
		Id = 1,
		ShipId = "01",
		QuotationType = "1",
		TransOrderYn = "N",
		QuotationNo = "Q220206001",
		ProdNo = "Apple",
		Qty = 1000,
		Price = 40000,
		Done = false
	});

	inputList.Add(new InputClass()
	{
		Id = 2,
		ShipId = "01",
		QuotationType = "1",
		TransOrderYn = "N",
		QuotationNo = "Q220206001",
		ProdNo = "Banana",
		Qty = 200,
		Price = 20,
		Done = false
	});

	inputList.Add(new InputClass()
	{
		Id = 3,
		ShipId = "02",
		QuotationType = "1",
		TransOrderYn = "N",
		QuotationNo = "Q220206001",
		ProdNo = "Candy",
		Qty = 200,
		Price = 20,
		Done = false
	});

	inputList.Add(new InputClass()
	{
		Id = 4,
		ShipId = "02",
		QuotationType = "1",
		TransOrderYn = "Y",
		QuotationNo = "Q220206001",
		ProdNo = "Candy",
		Qty = 200,
		Price = 20,
		Done = false
	});

	return inputList;
}

/// <summary>
/// 檢查特定機型的數量是否大於貨櫃上限數量.
/// </summary>
/// <param name="partNo">The partNo<see cref="string"/>機型.</param>
/// <param name="purQty">The purQty<see cref="decimal"/>採購數量.</param>
/// <returns>The <see cref="bool"/>.</returns>
static (bool, decimal?) CheckSpecificPartNum(string partNo, decimal? purQty)
{
	switch (partNo.ToUpper())
	{
		case "APPLE":
			return (purQty > 200, 200);
		case "BANANA":
			return (purQty > 300, 300);
		default:
			return (false, 0);
	}
}


public class InputClass
{
	public int Id { get; set; } // 編號
	public decimal? TotalPrice { get; set; }    //	總金額
	public string ProdNo { get; set; }  // 機型
	public decimal? Qty { get; set; }   //	數量
	public decimal? Price { get; set; } //	單價
	public bool Done { get; set; } = false; //	轉換完畢
	public string TransOrderYn { get; set; }	//	是否已轉訂單。 Y: 已轉, N: 未轉
	public string QuotationType { get; set; }	//	報價單種類。 1:一般報價單, 2:特殊報價單
	public string QuotationNo { get; set; } //	報價單編號
	public string ShipId { get; set; } //	運送方式編號。	1: 海運, 2: 陸運



}