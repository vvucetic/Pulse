#Pulse
Background job engine

##Client

Enqueue job just like executing any other method. Method will be serialized with passed parameters and executed on worker in server.

```C#
GlobalConfiguration.Configuration.UseSqlServerStorage("db");
var client = new BackgroundJobClient();
client.Enqueue(() => Method(1, DateTime.UtcNow));

public void Method(int i, DateTime date)
{
    Thread.Sleep(5000);
    Debug.WriteLine("Wooorks!");
    Debug.WriteLine("Nooo!");
    throw new Exception("Custom exception");
}
```

Besides that, it's possible to enqueue complex workflow of tasks like this:

```C#
var dl = WorkflowJob.MakeJob(() => WorkflowMethod("Download"));
var co1 = WorkflowJob.MakeJob(() => WorkflowMethod("Do something 1"));
var co2 = WorkflowJob.MakeJob(() => WorkflowMethod("Do something 2"));
var co3 = WorkflowJob.MakeJob(() => WorkflowMethod("Do something 3"));
var se1 = WorkflowJob.MakeJob(() => WorkflowMethod("Send email 1"));
var se3 = WorkflowJob.MakeJob(() => WorkflowMethod("Send email 3"));
var de = WorkflowJob.MakeJob(() => WorkflowMethod("Clean up"));

de.WaitFor(se1, co2, se3);
co1.ContinueWith(se1);
co3.ContinueWith(se3);
var group = WorkflowJobGroup.RunInParallel(co1, co2, co3);
dl.ContinueWithGroup(group);

var wf = new Workflow(dl);
GlobalConfiguration.Configuration.UseSqlServerStorage("db");
RecurringWorkflow.AddOrUpdate("test workflow", wf, Cron.MinuteInterval(1));
```

Execution will like this:


