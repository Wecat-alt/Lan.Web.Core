import useSocketStore from '@/stores/socket'

export default {
  onMessage(connection) {
    connection.on('ReceiveTargetData', (data) => {
      useSocketStore().getTarget(data)
    })

    connection.on('TrackTargetData', (data) => {

    })

  }
}
