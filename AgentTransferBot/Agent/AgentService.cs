﻿using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Internals;
using Microsoft.Bot.Connector;
using System.Threading;
using System.Threading.Tasks;
using static AgentTransferBot.Utilities;

namespace AgentTransferBot
{
    public class AgentService : IAgentService
    {
        private readonly IAgentProvider _agentProvider;
        private readonly IBotDataStore<BotData> _botDataStore;

        public AgentService(IAgentProvider agentProvider, IBotDataStore<BotData> botDataStore, IActivity message)
        {
            _agentProvider = agentProvider;
            _botDataStore = botDataStore;
        }

        public async Task<bool> IsInExistingConversationAsync(IActivity activity, CancellationToken cancellationToken)
        {
            var user = await GetUserFromAgentStateAsync(Address.FromActivity(activity), cancellationToken);
            if (user == null)
                return false;
            return true;
        }

        public async Task<bool> RegisterAgentAsync(IActivity activity, CancellationToken cancellationToken)
        {
            var result = _agentProvider.AddAgent(new Agent(activity));
            if(result)
                await SetAgentMetadataInStateAsync(Address.FromActivity(activity), cancellationToken);
            return result;
        }

        public async Task<bool> UnregisterAgentAsync(IActivity activity, CancellationToken cancellationToken)
        {
            var agent = _agentProvider.RemoveAgent(new Agent(activity));
            if (agent == null)
                return false;
            return true;
        }

        public async Task<AgentMetaData> GetAgentMetadataAsync(IAddress agentAddress, CancellationToken cancellationToken)
        {
            var botData = await GetBotDataAsync(agentAddress, _botDataStore, cancellationToken);
            AgentMetaData agentMetaData;
            botData.UserData.TryGetValue(Constants.AGENT_METADATA_KEY, out agentMetaData);
            return agentMetaData;
        }

        public async Task StopAgentUserConversationAsync(IAddress userAddress, IAddress agentAddress, CancellationToken cancellationToken)
        {
            var userData = await GetBotDataAsync(userAddress, _botDataStore, cancellationToken);
            var agentData = await GetBotDataAsync(agentAddress, _botDataStore, cancellationToken);

            userData.PrivateConversationData.RemoveValue(Constants.AGENT_KEY);
            agentData.PrivateConversationData.RemoveValue(Constants.USER_KEY);
        }

        public async Task<User> GetUserFromAgentStateAsync(IAddress agentAddress, CancellationToken cancellationToken)
        {
            var botData = await GetBotDataAsync(agentAddress, _botDataStore, cancellationToken);
            User user;
            botData.PrivateConversationData.TryGetValue(Constants.USER_KEY, out user);
            return user;
        }

        public async Task<Agent> GetAgentFromUserStateAsync(IAddress userAddress, CancellationToken cancellationToken)
        {
            var botData = await GetBotDataAsync(userAddress, _botDataStore, cancellationToken);
            Agent agent;
            botData.PrivateConversationData.TryGetValue(Constants.AGENT_KEY, out agent);
            return agent;
        }

        private async Task SetAgentMetadataInStateAsync(IAddress agentAddress, CancellationToken cancellationToken)
        {
            var botData = await GetBotDataAsync(agentAddress, _botDataStore, cancellationToken);
            botData.UserData.SetValue(Constants.AGENT_METADATA_KEY, new AgentMetaData() { IsAgent = true });
            await botData.FlushAsync(cancellationToken);
        }
    }
}
