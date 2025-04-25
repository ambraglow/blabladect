using Busmail;

namespace API.API_FP_MM {
    public class API_FP_MM {
        private readonly MessageBus _bus;
        public API_FP_MM(MessageBus bus) {
            _bus = bus;
        }
        private enum ManagementCommands : ushort {
            GET_ID_REQ = 0x4004,
            GET_ID_CFM,
            GET_MODEL_REQ,
            GET_MODEL_CFM,
            SET_ACCESS_CODE_REQ,
            SET_ACCESS_CODE_CFM,
            GET_ACCESS_CODE_REQ,
            GET_ACCESS_CODE_CFM,
            GET_REGISTRATION_COUNT_REQ = 0x4100,
            GET_REGISTRATION_COUNT_CFM,
            GET_HANDSET_IPUI_REQ,
            GET_HANDSET_IPUI_CFM,
            HANDSET_PRESENT_IND,
            STOP_PROTOCOL_REQ = 0x410B,
            START_PROTOCOL_REQ,
            HANDSET_DETACH_IND,
            EXT_HIGHER_LAYER_CAP2_REQ,
            GET_NAME_REQ,
            GET_NAME_CFM,
            SET_NAME_REQ,
            SET_NAME_CFM,
        }
        private enum RegistrationCommands {
            SET_REGISTRATION_MODE_REQ = 0x4105,
            SET_REGISTRATION_MODE_CFM,
            REGISTRATION_COMPLETE_IND,
            REGISTRATION_FAILED_IND = 0x4104,
            DELETE_REGISTRATION_REQ = 0x4102,
            DELETE_REGISTRATION_CFM,
            HANDSET_DERIGESTERED_IND = 0x410F,
            REGISTRATION_MODE_DISABLED_IND = 0x4114
        }
        public enum ApiMmRejectReasonType {
            REJ_NO_REASON,
            REJ_TPUI_UNKNOWN,
            REJ_IPUI_UKNOWN // need to finish filling this out
        }
        public enum ApiMmProtocolPcmSyncType {
            START_PROTOCOL_PCM_SYNC_SLAVE,
            START_PROTOCOL_PCM_SYNC_MASTER,
            START_PROTOCOL_PCM_SYNC_SLAVE_1_FS_DELAY
        }
        public enum ApiMmRegistrationModeType {
            REGISTRATION_MODE_DISABLED,
            REGISTRATION_MODE_CONTINOUS,
            REGISTRATION_MODE_SINGLE
        }
        internal void ApiFpMmGetIdReq() {
            Console.Write("Requesting unique FP ID: ");
            _bus.Transmit.InfoFrame( (ushort)ManagementCommands.GET_ID_REQ, false);
        }
    }
}