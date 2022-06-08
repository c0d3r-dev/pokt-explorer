using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;



namespace NodeOperations
{
	public class PoktClient
	{
		static readonly HttpClient Http = new HttpClient();

		public static async Task<long> QueryBalanceAsync(string Address, int Height = -1)
		{
			var Req = new HttpRequestMessage(HttpMethod.Post, "https://mainnet.gateway.pokt.network/v1/lb/XXXYOURIDXXX/v1/query/account");
			Req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", "XXXYOURPASSWORDXXX");
			Req.Content =
				Height > 0 ?
				new StringContent(JsonConvert.SerializeObject(new { address = Address, height = Height }), Encoding.UTF8, "application/json") :
				new StringContent(JsonConvert.SerializeObject(new { address = Address }), Encoding.UTF8, "application/json");
			var Res = await Http.SendAsync(Req);
			if (!Res.IsSuccessStatusCode)
				throw new Exception("Query failed.");

			var ResString = await Res.Content.ReadAsStringAsync();
			if (ResString == null || ResString == "null")
				return 0;

			dynamic ResStringParsed = JsonConvert.DeserializeObject(ResString);
			if (ResStringParsed.coins.Count == 0)
				return 0;

			return long.Parse(ResStringParsed.coins[0].amount.ToString());
		}

		public class Tx
		{
			public string Hash;
			public int Height;
			public string Type;
			public string FromAddress;
			public string ToAddress;
			public long Amount;
			public int TotalProofs;
			public int Code;
			public string Memo;
			public string Raw;
		}

		public static async Task<List<Tx>> QueryAccountTxsAsync(
			string Address, 
			int Page, 
			int PerPage, 
			bool IncludeReceived = true, 
			bool NewestFirst = true)
		{
			var Req = new HttpRequestMessage(HttpMethod.Post, "https://mainnet.gateway.pokt.network/v1/lb/XXXYOURIDXXX/v1/query/accounttxs");
			Req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", "XXXYOURPASSWORDXXX");
			Req.Content = new StringContent(JsonConvert.SerializeObject(new { address = Address, page = Page, per_page = PerPage, received = IncludeReceived, prove = false, order = NewestFirst ? "asc" : "desc" }), Encoding.UTF8, "application/json");
			var Res = await Http.SendAsync(Req);
			if (!Res.IsSuccessStatusCode)
				throw new Exception("Query failed.");

			var ResString = await Res.Content.ReadAsStringAsync();
			dynamic ResStringParsed = JsonConvert.DeserializeObject(ResString);

			List<Tx> Txs = new();
			for (int i = 0; i < ResStringParsed.txs.Count; ++i)
			{
				var CurTx = ResStringParsed.txs[i];
				Txs.Add(new Tx()
				{
					Hash = CurTx.hash.ToString(),
					Height = CurTx.height,
					Type = CurTx.stdTx.msg.type.ToString(),
					FromAddress = CurTx.stdTx.msg.value.from_address?.ToString(),
					ToAddress = CurTx.stdTx.msg.value.to_address?.ToString(),
					Amount = long.Parse(CurTx.stdTx.msg.value.amount?.ToString() ?? "0"),
					TotalProofs = int.Parse(CurTx.stdTx.msg.value.total_proofs?.ToString() ?? "0"),
					Memo = CurTx.stdTx.memo?.ToString(),
					Code = CurTx.tx_result.code,
					Raw = JsonConvert.SerializeObject(CurTx)
				});
			}
			return Txs;
		}

		public static async Task<Tx> QueryTxAsync(string Hash)
		{
			var Req = new HttpRequestMessage(HttpMethod.Post, "https://mainnet.gateway.pokt.network/v1/lb/XXXYOURIDXXX/v1/query/tx");
			Req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", "XXXYOURPASSWORDXXX");
			Req.Content = new StringContent(JsonConvert.SerializeObject(new { hash = Hash }), Encoding.UTF8, "application/json");
			var Res = await Http.SendAsync(Req);
			if (!Res.IsSuccessStatusCode)
				throw new Exception("Query failed.");

			var ResString = await Res.Content.ReadAsStringAsync();
			dynamic ResStringParsed = JsonConvert.DeserializeObject(ResString);

			return new Tx()
			{
				Hash = ResStringParsed.hash.ToString(),
				Height = ResStringParsed.height,
				Type = ResStringParsed.stdTx.msg.type.ToString(),
				FromAddress = ResStringParsed.stdTx.msg.value.from_address?.ToString(),
				ToAddress = ResStringParsed.stdTx.msg.value.to_address?.ToString(),
				Amount = long.Parse(ResStringParsed.stdTx.msg.value.amount?.ToString() ?? "0"),
				TotalProofs = int.Parse(ResStringParsed.stdTx.msg.value.total_proofs?.ToString() ?? "0"),
				Memo = ResStringParsed.stdTx.memo?.ToString(),
				Code = ResStringParsed.tx_result.code,
				Raw = JsonConvert.SerializeObject(ResStringParsed)
			};
		}

		public class QueryNode
		{
			public int Status;
			public bool Jailed;
			public long Tokens;
			public string UnstakingTime;
			public string ServiceUrl;
			public string[] Chains;
		}

		public static async Task<QueryNode> QueryNodeAsync(string Address, int Height = -1)
		{
			var Req = new HttpRequestMessage(HttpMethod.Post, "https://mainnet.gateway.pokt.network/v1/lb/XXXYOURIDXXX/v1/query/node");
			Req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", "XXXYOURPASSWORDXXX");

			object Query =
				Height <= 0 ?
				new { address = Address } :
				new { address = Address, height = Height };
			Req.Content = new StringContent(JsonConvert.SerializeObject(Query), Encoding.UTF8, "application/json");
			var Res = await Http.SendAsync(Req);
			if (!Res.IsSuccessStatusCode)
				throw new Exception("Error: Query failed.");

			string PocketQueryNodeString = await Res.Content.ReadAsStringAsync();
			if (PocketQueryNodeString.Contains("validator not found"))
				return null;

			dynamic PocketQueryNode = JsonConvert.DeserializeObject(PocketQueryNodeString);
			return new QueryNode()
			{
				Status = (int)PocketQueryNode.status,
				Jailed = (bool)PocketQueryNode.jailed,
				Tokens = (long)PocketQueryNode.tokens,
				UnstakingTime = (string)PocketQueryNode.unstaking_time,
				ServiceUrl = ((string)PocketQueryNode.service_url).ToLower(),
				Chains = PocketQueryNode.chains.ToString().Split(new char[] { '\r', '\n', ' ', ',', '"', '[', ']' }, StringSplitOptions.RemoveEmptyEntries)
			};
		}

		public static async Task<string> QueryParamAsync(string Param, int Height = -1)
		{
			var Req = new HttpRequestMessage(HttpMethod.Post, "https://mainnet.gateway.pokt.network/v1/lb/XXXYOURIDXXX/v1/query/param");
			Req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", "XXXYOURPASSWORDXXX");

			object Query =
				Height <= 0 ?
				new { key = Param } :
				new { key = Param, height = Height };
			Req.Content = new StringContent(JsonConvert.SerializeObject(Query), Encoding.UTF8, "application/json");
			var Res = await Http.SendAsync(Req);
			if (!Res.IsSuccessStatusCode)
				throw new Exception("Query failed.");

			return await Res.Content.ReadAsStringAsync();
		}

		public static async Task<string> QuerySupplyAsync(int Height = -1)
		{
			var Req = new HttpRequestMessage(HttpMethod.Post, "https://mainnet.gateway.pokt.network/v1/lb/XXXYOURIDXXX/v1/query/supply");
			Req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", "XXXYOURPASSWORDXXX");

			object Query =
				Height <= 0 ?
				new { } :
				new { height = Height };
			Req.Content = new StringContent(JsonConvert.SerializeObject(Query), Encoding.UTF8, "application/json");
			var Res = await Http.SendAsync(Req);
			if (!Res.IsSuccessStatusCode)
				throw new Exception("Query failed.");

			return await Res.Content.ReadAsStringAsync();
		}

		public static async Task<string> QueryStateAsync(int Height = -1)
		{
			var Req = new HttpRequestMessage(HttpMethod.Post, "https://mainnet.gateway.pokt.network/v1/lb/XXXYOURIDXXX/v1/query/state");
			Req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", "XXXYOURPASSWORDXXX");

			object Query =
				Height <= 0 ?
				new { } :
				new { Height = Height };
			Req.Content = new StringContent(JsonConvert.SerializeObject(Query), Encoding.UTF8, "application/json");
			var Res = await Http.SendAsync(Req);
			if (!Res.IsSuccessStatusCode)
				throw new Exception("Query failed.");

			return await Res.Content.ReadAsStringAsync();
		}
	}
}
