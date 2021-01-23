using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;

namespace OrchardCore.Deployment.Deployment
{
    public class DeploymentPlanDeploymentSource : IDeploymentSource
    {
        private readonly IDeploymentPlanService _deploymentPlanService;
        private readonly IServiceProvider _serviceProvider;

        public DeploymentPlanDeploymentSource(
            IDeploymentPlanService deploymentPlanService,
            IServiceProvider serviceProvider)
        {
            _deploymentPlanService = deploymentPlanService;
            _serviceProvider = serviceProvider;
        }

        public async Task ProcessDeploymentStepAsync(DeploymentStep deploymentStep, DeploymentPlanResult result)
        {
            if (!(deploymentStep is DeploymentPlanDeploymentStep deploymentPlanStep))
            {
                return;
            }

            if (!await _deploymentPlanService.DoesUserHavePermissionsAsync())
            {
                return;
            }

            var deploymentStepFactories = _serviceProvider.GetServices<IDeploymentStepFactory>().ToDictionary(f => f.Name);
      
            var deploymentPlans = deploymentPlanStep.IncludeAll
                ? (await _deploymentPlanService.GetAllDeploymentPlansAsync()).ToArray()
                : (await _deploymentPlanService.GetDeploymentPlansAsync(deploymentPlanStep.DeploymentPlanNames)).ToArray();

            var plans = (from plan in deploymentPlans
                         select new
                         {
                             plan.Name,
                             Steps = (from step in plan.DeploymentSteps
                                      select new
                                      {
                                          Type = GetStepType(deploymentStepFactories, step),
                                          Step = step
                                      }).ToArray()
                         }).ToArray();

            // Adding deployment plans
            result.Steps.Add(new JObject(
                new JProperty("name", "deployment"),
                new JProperty("Plans", JArray.FromObject(plans))
            ));
        }

        /// <summary>
        /// A Site Setting Step is generic and the name is mapped to the <see cref="IDeploymentStepFactory.Name"/> so its 'Type' should be determined though a lookup.
        /// A normal steps name is not mapped to the <see cref="IDeploymentStepFactory.Name"/> and should use its type.
        /// </summary>
        private string GetStepType(IDictionary<string, IDeploymentStepFactory> deploymentStepFactories, DeploymentStep step)
        {
            if (deploymentStepFactories.TryGetValue(step.Name, out var deploymentStepFactory))
            {
                return deploymentStepFactory.Name;
            }
            else
            {
                return step.GetType().Name;
            }
        }
    }
}
