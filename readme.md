# Pulse
Pulse is background job engine for executing jobs in background in reliable way, resistant to host (application or machine) restarts or power loss because its baked by persistend storage. It is inspired and unofficially forked from Hangfire (Hangfire is production ready, unlike Pulse).

## Client

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

Besides that, it's possible to enqueue complex workflow of jobs like this:

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
var client = new BackgroundJobClient();
client.CreateAndEnqueue(wf);
```

Execution will like this:

![Workflow diagram](https://raw.githubusercontent.com/vvucetic/Pulse/master/Assets/workflow.png)

### Recurring

Engine support recurring task for both jobs and workflows. Recurring jobs will be enqueued at the planned time. Cron sintax is supported.

For jobs:
```C#
RecurringJob.AddOrUpdate(() => RecurringMethod("Recurring task", 1), Cron.MinuteInterval(2));
```

or for workflows:
```C#
RecurringWorkflow.AddOrUpdate("test workflow", wf, Cron.MinuteInterval(1));
```

## Server

Server can be run in other process, windows service, console application or ASP.NET application.

```C#
GlobalConfiguration.Configuration.UseSqlServerStorage("db");
var server = new BackgroundJobServer();
```

## Retry policy

If failed, each job is retried configured number of times with exponential backoff. After failure, job is delayed and requeued on scheduled time by engine background process. Each job can be configured to custom number of retries. 

## Parallelism

By default, engine has 20 workers that pop from the queue in parallel manner. For greather scale, server can be run on multiple machines.

## Heartbeat and watchdog

Each server registers his worker ids and heartbeats every minute. If server hasn't heartbeated for more than 5 minutes, engine will automatically remove server from server list and all jobs registered to be running on workers of that server will automatically enqueue for another run. This way, engine ensures at least once delivery (with possible delay of timeout).