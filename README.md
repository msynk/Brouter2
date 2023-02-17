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