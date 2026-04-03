export function handall() {
        listRadar().then((response) => {
          this.radarOptions = response.data.data;

          if(this.y_Id!=''){
              this.queryParams.id =this.y_Id;
          }else{
            this.queryParams.id = this.radarOptions[0].id;
            this.y_Id=this.radarOptions[0].id;
          }
    
          var tt= this.radarOptions.find(item => item.id === this.queryParams.id);

          this.queryParams.angle=tt.defenceAngle;
          this.queryParams.radius=tt.defenceRadius;
          this.queryParams.northDeviationAngle=tt.northDeviationAngle;
          this.queryParams.radarLat=tt.latitude;
          this.queryParams.radarLon=tt.longitude;

          for (let item of this.radarOptions) {
            this.radarDraw.radius= item.defenceRadius,//探测距离
            this.radarDraw.angle=item.defenceAngle,
            this.radarDraw.northDeviationAngle= item.northDeviationAngle, //旋转角度
            this.radarDraw.lat=item.latitude,
            this.radarDraw.lng=item.longitude,
            this.radarDraw.step=item.status == 1 ? 4 : 0;  
           

            this.initRadar();
          }    
        })
      }