# Brouter2
An awesome modern router for Blazor.


```razor
<SBrouter NotFound="404">
    <Route Template="/" RedirectTo="/home" />

    <Route Template="/home">
        <Content><HomePage CountValue="@currentCount"></HomePage></Content>
    </Route>

    <Route Template="/counter">
        <Content><CounterPage @bind-CurrentCount="currentCount" ChangeCount="@ChangeCountValue" /></Content>
    </Route>

    <Route Template="/counter/{init:int}">
        <Content><CounterPage @bind-CurrentCount="currentCount" ChangeCount="@ChangeCountValue" /></Content>
    </Route>

    <Route Template="/counter/multi/{id:int:long}/{age:long:decimal:double}/{name}">
        <Content><CounterPage @bind-CurrentCount="currentCount" ChangeCount="@ChangeCountValue" /></Content>
    </Route>

    <Route Template="/fetchdata" Component="typeof(FetchDataPage)" />

    <Route Template="/*/test">
        <Content><p>Test page</p></Content>
    </Route>
    <Route Template="/*/test">
        <Content><p>Test page 2 [@context.Count]</p></Content>
    </Route>

    <Route Template="/{prefix}/test2/{postfix}">
        <Content><p>[@context["prefix"]] Test2 page [@context["postfix"]]</p></Content>
    </Route>

    <Route Template="/nested-route">
        <Route Template="/{id:int}" Component="@typeof(FetchDataPage)" />
        <Route Template="/{id:int}/hello">
            <Content>
                <div>This a nested-route (/{id:int}/hello)</div>
                <div>id: [@context["id"]]</div>
                <hr />
                <FetchDataPage Value="2" />
            </Content>
        </Route>
        <Route Template="/{id:int}/world">
            <Content>
                <div>This a nested-route (/{id:int}/world)</div>
                <div>id: [@context["id"]]</div>
                <hr /><FetchDataPage Value="3" />
            </Content>
        </Route>

        <Route Template="/nested-2">
            <Route Template="/{id:int}" Component="@typeof(FetchDataPage)" />
        </Route>
    </Route>

    <Route Template="g1" Guard="() => DateTime.Now.Second % 2 == 0" RedirectTo="403">
        <Content>This is <strong>tt1</strong> route</Content>
    </Route>
    <Route Template="g2" Guard="() => DateTime.Now.Minute % 2 == 0" RedirectTo="403">
        <Content>This is <strong>tt2</strong> route</Content>
    </Route>

    <Route Template="nested">
        <Content><Nested></Nested></Content>
    </Route>

    <Route Template="404">
        <Content>
            <h1 class="text-danger">404</h1>
            <p>Sorry, there's nothing at this address.</p>
        </Content>
    </Route>

    <Route Template="/403">
        <Content>
            <h1 class="text-danger">403.1 oops!</h1>
            <p>Sorry, you can't go there yet.</p>
        </Content>
    </Route>
</SBrouter>
```


Counter page:

```razor
<h1>Counter</h1>

<p>Current count: @CurrentCount</p>

<button class="btn btn-primary" @onclick="() => ChangeCount?.Invoke(CurrentCount + 1)">Click me</button>

<br />

<div>Init: @Init</div>

<br />

<div>Id: @Id</div>
<div>Age: @Age</div>
<div>Name: @Name</div>

@code {
    [Parameter] public int CurrentCount { get; set; } = 0;
    [Parameter] public EventCallback<int> CurrentCountChanged { get; set; }
    [Parameter] public Action<int> ChangeCount { get; set; }

    [CascadingParameter(Name = "RouteParameters")] IDictionary<string, object> Parameters { get; set; }

    [CascadingParameter(Name = "init")] int Init { get; set; }

    [CascadingParameter(Name = "id")] long Id { get; set; }
    [CascadingParameter(Name = "age")] double Age { get; set; }
    [CascadingParameter(Name = "name")] object Name { get; set; }


    private bool parameterHasSet = false;

    protected override void OnParametersSet()
    {
        if (parameterHasSet) return;

        parameterHasSet = true;

        base.OnParametersSet();

        if (Parameters is not null && Parameters.ContainsKey("init"))
        {
            CurrentCount = (int)Parameters["init"];
            ChangeCount(CurrentCount);
        }
    }
}
```

FetchData page:

```razor
@inject HttpClient Http

<h1>Weather forecast</h1>

<p>This component demonstrates fetching data from the server.</p>

@if (forecasts == null)
{
    <p><em>Loading...</em></p>
}
else
{
    if (Parameters?.ContainsKey("id") ?? false)
    {
        <p>Id: @Parameters["id"], Value: @Value</p>
    }
    <table class="table">
        <thead>
            <tr>
                <th>Date</th>
                <th>Temp. (C)</th>
                <th>Temp. (F)</th>
                <th>Summary</th>
            </tr>
        </thead>
        <tbody>
            @foreach (var forecast in forecasts)
            {
                <tr>
                    <td>@forecast.Date.ToShortDateString()</td>
                    <td>@forecast.TemperatureC</td>
                    <td>@forecast.TemperatureF</td>
                    <td>@forecast.Summary</td>
                </tr>
            }
        </tbody>
    </table>
}

@code {
    [Parameter] public string Value { get; set; } = "N/A";

    [CascadingParameter(Name = "RouteParameters")] IDictionary<string, object> Parameters { get; set; }

    private WeatherForecast[] forecasts;

    protected override async Task OnInitializedAsync()
    {
        forecasts = await Http.GetFromJsonAsync<WeatherForecast[]>("sample-data/weather.json");
    }

    public class WeatherForecast
    {
        public DateTime Date { get; set; }

        public int TemperatureC { get; set; }

        public string Summary { get; set; }

        public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
    }
}

```

Nested page:

```razor
<h1>Nested route: /nested</h1>

<p>lets test some nested routes</p>

<button @onclick="() => count++">Count: [@count]</button>

<hr />

<a href="/nested/n1/@count">"/nested/n1/@count"</a>

<hr />

<Route Template="n1/{count:int}">
    <Content>
        <h3>this is the nested route /n1</h3>
        <div>count: [@context["count"]]</div>
    </Content>
</Route>

@code {
    private int count;
}
```